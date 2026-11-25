using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Graphics.Drawables;
using AndroidX.Core.Content;
using Google.Android.Material.AppBar;

namespace com.aa.tvshows
{
    /// <summary>
    /// A show's page: backdrop, synopsis, My List, and one season of episodes at a time
    /// (each season is fetched the first time it is opened).
    /// </summary>
    [Activity(Label = "TV Show Detail", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ShowDetailActivity : AppCompatActivity
    {
        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.anim_pop_enter, Resource.Animation.anim_pop_exit);
        }

        ImageView backdrop;
        TextView titleText, ratingText, metaText, overviewText, listButton, seasonButton;
        RecyclerView episodesView;
        ProgressBar progress;
        TextView noEpisodesText;
        EpisodeRowsAdapter episodeAdapter;
        View topBar;
        ColorDrawable topBarBackground;
        TextView topBarTitle;

        SeriesDetails show;
        bool inList;
        bool overviewExpanded;
        SeasonData currentSeason;
        int loadVersion;
        readonly Dictionary<int, List<ShowEpisodeDetails>> seasonCache = new Dictionary<int, List<ShowEpisodeDetails>>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            WindowInsetsHelper.EnableEdgeToEdge(this);
            SetContentView(Resource.Layout.activity_show_detail);

            backdrop = FindViewById<ImageView>(Resource.Id.detail_backdrop);
            titleText = FindViewById<TextView>(Resource.Id.detail_title);
            ratingText = FindViewById<TextView>(Resource.Id.detail_rating);
            metaText = FindViewById<TextView>(Resource.Id.detail_meta);
            overviewText = FindViewById<TextView>(Resource.Id.detail_overview);
            listButton = FindViewById<TextView>(Resource.Id.detail_btn_list);
            seasonButton = FindViewById<TextView>(Resource.Id.detail_btn_season);
            episodesView = FindViewById<RecyclerView>(Resource.Id.detail_episodes);
            progress = FindViewById<ProgressBar>(Resource.Id.detail_progress);
            noEpisodesText = FindViewById<TextView>(Resource.Id.detail_no_episodes);

            topBar = FindViewById<View>(Resource.Id.detail_top_bar);
            var back = FindViewById<View>(Resource.Id.detail_back);
            var appBar = FindViewById<AppBarLayout>(Resource.Id.detail_appbar);
            if (topBar != null && back != null && appBar != null)
            {
                topBarTitle = FindViewById<TextView>(Resource.Id.detail_top_bar_title);
                topBarBackground = new ColorDrawable(new Android.Graphics.Color(ContextCompat.GetColor(this, Resource.Color.colorNav)))
                {
                    Alpha = 0
                };
                topBar.Background = topBarBackground;
                WindowInsetsHelper.Apply(topBar, top: true);

                back.Click += (s, e) => AppView.FinishWithTransition(this);
                appBar.AddOnOffsetChangedListener(new TopBarFadeListener(this));
            }
            WindowInsetsHelper.Apply(episodesView, bottom: true);

            episodesView.SetLayoutManager(new LinearLayoutManager(this));
            episodeAdapter = new EpisodeRowsAdapter();
            episodesView.SetAdapter(episodeAdapter);

            // nothing shows until the data it would display has actually arrived
            titleText.Visibility = ViewStates.Invisible;
            ratingText.Visibility = ViewStates.Invisible;
            metaText.Visibility = ViewStates.Invisible;
            overviewText.Visibility = ViewStates.Invisible;
            listButton.Visibility = ViewStates.Invisible;
            episodesView.Visibility = ViewStates.Invisible;

            listButton.Click += async (s, e) => await ToggleListAsync();
            seasonButton.Click += (s, e) => ShowSeasonMenu();
            overviewText.Click += (s, e) =>
            {
                // phone: tap the synopsis to read all of it
                overviewExpanded = !overviewExpanded;
                overviewText.SetMaxLines(overviewExpanded ? int.MaxValue : 4);
            };

            _ = LoadAsync(Intent.GetStringExtra("itemLink"));
        }

        async Task LoadAsync(string link)
        {
            progress.Visibility = ViewStates.Visible;
            SeriesDetails loaded = null;
            try
            {
                loaded = await TMDbClient.GetDetailsForTVShowSeries(link, includeEpisodes: false);
            }
            catch (Exception)
            {
            }
            if (IsDestroyed) return;
            progress.Visibility = ViewStates.Gone;

            if (loaded == null)
            {
                Error.Instance.ShowErrorTip("This show could not be loaded.", this);
                titleText.Text = GetString(Resource.String.home_load_error);
                titleText.Visibility = ViewStates.Visible;
                return;
            }

            show = loaded;
            Bind();
            inList = await StorageData.IsMarkedFavorite(show);
            ShowListState();

            if (show.Seasons != null && show.Seasons.Count > 0)
            {
                await ShowSeasonAsync(DefaultSeason(), isAutoSelect: true);
            }
            else
            {
                Error.Instance.ShowErrorTip(GetString(Resource.String.detail_no_episodes), this);
            }
            listButton.RequestFocus();
        }

        void Bind()
        {
            AppView.LoadPicture(backdrop, !string.IsNullOrEmpty(show.BackdropLink) ? show.BackdropLink : show.ImageLink);
            titleText.Text = show.Title;
            if (topBarTitle != null) topBarTitle.Text = show.Title;

            if (string.IsNullOrEmpty(show.Rating))
            {
                ratingText.Visibility = ViewStates.Gone;
            }
            else
            {
                ratingText.Text = show.Rating;
                ratingText.Visibility = ViewStates.Visible;
            }

            var meta = new List<string>();
            if (!string.IsNullOrEmpty(show.ReleaseDate) && show.ReleaseDate.Length >= 4) meta.Add(show.ReleaseDate.Substring(0, 4));
            if (!string.IsNullOrEmpty(show.Status)) meta.Add(show.Status);
            if (!string.IsNullOrEmpty(show.Genres)) meta.Add(show.Genres);
            metaText.Text = string.Join("  •  ", meta);

            overviewText.Text = string.IsNullOrWhiteSpace(show.Description) ? GetString(Resource.String.detail_overview_missing) : show.Description;

            titleText.Visibility = ViewStates.Visible;
            metaText.Visibility = ViewStates.Visible;
            overviewText.Visibility = ViewStates.Visible;
            listButton.Visibility = ViewStates.Visible;
        }

        /// <summary>Honors the "latest first" setting: open on the newest season that actually has
        /// episodes listed, otherwise the first such season. TMDB sometimes lists a season (e.g. an
        /// upcoming/announced one) with zero episodes, so jumping straight to it would open on an
        /// empty list; seasons with no episodes are skipped when picking the default. Falls back to
        /// any numbered season if none of them report episodes yet.</summary>
        SeasonData DefaultSeason()
        {
            var numbered = show.Seasons.Where(s => s.SeasonNumber > 0).ToList();
            if (numbered.Count == 0) return show.Seasons[0];

            var withEpisodes = numbered.Where(s => s.EpisodeCount > 0).ToList();
            var candidates = withEpisodes.Count > 0 ? withEpisodes : numbered;

            return StorageData.GetShowLatestEpisodesFirstSetting()
                ? candidates.OrderByDescending(s => s.SeasonNumber).First()
                : candidates.OrderBy(s => s.SeasonNumber).First();
        }

        /// <summary>The next season to try, in the configured direction, excluding ones already tried.
        /// Used when a season's EpisodeCount said it had episodes but TMDB's own episode list for it
        /// still came back empty (metadata and listing can disagree while a season is mid-rollout).</summary>
        SeasonData NextFallbackSeason(HashSet<int> tried)
        {
            var numbered = show.Seasons.Where(s => s.SeasonNumber > 0 && !tried.Contains(s.SeasonNumber)).ToList();
            if (numbered.Count == 0) return null;
            return StorageData.GetShowLatestEpisodesFirstSetting()
                ? numbered.OrderByDescending(s => s.SeasonNumber).First()
                : numbered.OrderBy(s => s.SeasonNumber).First();
        }

        void ShowSeasonMenu()
        {
            if (show?.Seasons == null) return;

            var menu = new AndroidX.AppCompat.Widget.PopupMenu(this, seasonButton);
            for (var i = 0; i < show.Seasons.Count; i++)
            {
                menu.Menu.Add(0, i, i, show.Seasons[i].SeasonName);
            }
            menu.MenuItemClick += async (s, e) => await ShowSeasonAsync(show.Seasons[e.Item.ItemId]);
            menu.Show();
        }

        async Task ShowSeasonAsync(SeasonData season, bool isAutoSelect = false, HashSet<int> triedSeasons = null)
        {
            if (season == null) return;

            currentSeason = season;
            seasonButton.Text = season.SeasonName;
            seasonButton.Visibility = show.Seasons.Count > 1 ? ViewStates.Visible : ViewStates.Gone;

            var current = ++loadVersion;
            if (!seasonCache.TryGetValue(season.SeasonNumber, out var episodes))
            {
                episodeAdapter.SetItems(null);
                progress.Visibility = ViewStates.Visible;
                try
                {
                    episodes = await TMDbClient.GetSeasonEpisodes(season);
                }
                catch (Exception)
                {
                    episodes = null;
                }
                if (current != loadVersion || IsDestroyed) return;
                progress.Visibility = ViewStates.Gone;

                if (episodes == null)
                {
                    Error.Instance.ShowErrorTip("Episodes could not be loaded.", this);
                    return;
                }
                seasonCache[season.SeasonNumber] = episodes;
            }

            // Only auto-selected opens fall back -- a season the person picked from the menu on
            // purpose still shows empty if that's genuinely what it is. TMDB's own episode_count can
            // say a season has episodes while the episode list itself comes back empty (a season
            // mid-rollout), so this is a second check beyond DefaultSeason()'s EpisodeCount filter.
            if (episodes.Count == 0 && isAutoSelect)
            {
                triedSeasons ??= new HashSet<int>();
                triedSeasons.Add(season.SeasonNumber);
                var fallback = NextFallbackSeason(triedSeasons);
                if (fallback != null)
                {
                    await ShowSeasonAsync(fallback, isAutoSelect: true, triedSeasons);
                    return;
                }
            }

            if (episodes.Count == 0)
            {
                episodeAdapter.SetItems(null);
                episodesView.Visibility = ViewStates.Gone;
                if (noEpisodesText != null) noEpisodesText.Visibility = ViewStates.Visible;
                return;
            }
            if (noEpisodesText != null) noEpisodesText.Visibility = ViewStates.Gone;

            var ordered = StorageData.GetShowLatestEpisodesFirstSetting()
                ? episodes.OrderByDescending(e => e.EpisodeIndex)
                : episodes.OrderBy(e => e.EpisodeIndex);
            episodeAdapter.SetItems(ordered);
            episodesView.ScrollToPosition(0);
            episodesView.Visibility = ViewStates.Visible;
        }

        void ShowListState()
        {
            listButton.Text = GetString(inList ? Resource.String.action_in_list : Resource.String.action_add_list);
            listButton.SetCompoundDrawablesRelativeWithIntrinsicBounds(inList ? Resource.Drawable.ic_check : Resource.Drawable.ic_add, 0, 0, 0);
        }

        async Task ToggleListAsync()
        {
            if (show == null) return;

            listButton.Enabled = false;
            try
            {
                var ok = inList
                    ? await StorageData.RemoveSeriesFromFavoritesFile(show)
                    : await StorageData.SaveSeriesToFavoritesFile(show);
                if (ok)
                {
                    inList = !inList;
                    ShowListState();
                }
                else
                {
                    Error.Instance.ShowErrorTip("My List could not be changed.", this);
                }
            }
            finally
            {
                listButton.Enabled = true;
            }
        }

        class TopBarFadeListener : Java.Lang.Object, AppBarLayout.IOnOffsetChangedListener
        {
            readonly ShowDetailActivity owner;

            public TopBarFadeListener(ShowDetailActivity owner)
            {
                this.owner = owner;
            }

            public void OnOffsetChanged(AppBarLayout appBarLayout, int verticalOffset)
            {
                int range = appBarLayout.TotalScrollRange;
                if (range <= 0 || owner.topBarBackground == null) return;

                var fadeDistance = Math.Max(1, range - (int)(240 * owner.Resources.DisplayMetrics.Density));
                var ratio = Math.Min(1f, (float)Math.Abs(verticalOffset) / fadeDistance);
                owner.topBarBackground.Alpha = (int)(ratio * 255);
                if (owner.topBarTitle != null) owner.topBarTitle.Alpha = ratio;
            }
        }
    }
}
