using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Fragments
{
    /// <summary>
    /// Netflix-style home: a hero for the featured show above horizontal rows.
    /// Phone: the hero is the list's own first item, so it scrolls away 1:1 with touch; a fixed
    /// bar above it carries the wordmark / Genres / Settings and fades its background in once the
    /// hero has scrolled behind it.
    /// Tv: the hero is a fixed backdrop that follows whichever tile has focus.
    /// </summary>
    public class HomeFragment : Fragment
    {
        bool isTv;
        View heroContainer; // tv only: the whole fixed hero, hidden until the first row arrives
        ImageView heroImage;
        TextView heroTitle, heroMeta, heroOverview, heroDetails, heroList;
        TextView tvHeroRating; // TV hero only; the phone hero's rating chip is bound inside HeroHolder
        View topBar;
        ColorDrawable topBarBackground;
        int topBarFadeDistancePx;
        RecyclerView rows;
        ProgressBar progress;
        LinearLayoutCompat emptyView;
        HomeRowsAdapter adapter;

        MediaCard heroCard;
        bool heroInList;
        int loadVersion;
        CancellationTokenSource focusDebounce;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            isTv = UiMode.IsTv(Context);
            var view = inflater.Inflate(Resource.Layout.fragment_home, container, false);

            rows = view.FindViewById<RecyclerView>(Resource.Id.home_rows);
            progress = view.FindViewById<ProgressBar>(Resource.Id.home_progress);
            emptyView = view.FindViewById<LinearLayoutCompat>(Resource.Id.home_empty_view);

            rows.SetLayoutManager(new LinearLayoutManager(Context));
            adapter = new HomeRowsAdapter(isTv);
            adapter.CardClick += card => AppView.HandleItemShowEpisodeClick(card, Context);
            adapter.CardFocused += OnCardFocused;
            adapter.ViewAllClick += OnViewAllClick;
            rows.SetAdapter(adapter);

            if (isTv)
            {
                // TV keeps the fixed backdrop-behind-rows layout; the hero views live directly in fragment_home.xml
                heroContainer = view;
                heroImage = view.FindViewById<ImageView>(Resource.Id.hero_image);
                heroTitle = view.FindViewById<TextView>(Resource.Id.hero_title);
                tvHeroRating = view.FindViewById<TextView>(Resource.Id.hero_rating);
                heroMeta = view.FindViewById<TextView>(Resource.Id.hero_meta);
                heroOverview = view.FindViewById<TextView>(Resource.Id.hero_overview);
                heroDetails = view.FindViewById<TextView>(Resource.Id.hero_btn_details);
                heroList = view.FindViewById<TextView>(Resource.Id.hero_btn_list);
                heroDetails.Click += (s, e) => { if (heroCard != null) AppView.HandleItemShowEpisodeClick(heroCard, Context); };
                heroList.Click += async (s, e) => await ToggleHeroListAsync();
            }
            else
            {
                // phone: hero is adapter item 0 (see item_home_hero.xml / HeroHolder); this fixed bar floats above it
                topBar = view.FindViewById<View>(Resource.Id.hero_top_bar);
                topBarBackground = new ColorDrawable(new Android.Graphics.Color(ContextCompat.GetColor(Context, Resource.Color.colorNav)))
                {
                    Alpha = 0
                };
                topBar.Background = topBarBackground;
                WindowInsetsHelper.Apply(topBar, top: true);
                topBarFadeDistancePx = (int)(Context.Resources.GetDimension(Resource.Dimension.hero_height)
                    - 120 * Context.Resources.DisplayMetrics.Density);

                view.FindViewById<View>(Resource.Id.hero_genres_btn).Click += (s, e) => AppView.StartActivityWithTransition(Context, new Intent(Context, typeof(GenresActivity)));
                view.FindViewById<View>(Resource.Id.hero_settings_btn).Click += (s, e) => AppView.StartActivityWithTransition(Context, new Intent(Context, typeof(SettingsActivity)));

                adapter.HeroDetailsClick += () => { if (heroCard != null) AppView.HandleItemShowEpisodeClick(heroCard, Context); };
                adapter.HeroListClick += () => _ = ToggleHeroListAsync();

                rows.AddOnScrollListener(new TopBarFadeListener(this));
            }

            _ = LoadAsync();
            return view;
        }

        public override void OnResume()
        {
            base.OnResume();
            _ = RefreshHeroListStateAsync();
        }

        public override void OnDestroyView()
        {
            loadVersion++;
            focusDebounce?.Cancel();
            base.OnDestroyView();
        }

        async Task LoadAsync()
        {
            var version = ++loadVersion;
            adapter.Clear();
            heroCard = null;
            if (isTv) heroContainer.Visibility = ViewStates.Invisible;
            AppView.SetEmptyView(emptyView, false);
            progress.Visibility = ViewStates.Visible;

            var requests = TMDbClient.StartHomeRowRequests();
            var anyRow = false;
            foreach (var request in requests)
            {
                CardPage page = null;
                try
                {
                    page = await request.Data;
                }
                catch (Exception)
                {
                    // one failed row must not take the whole screen down
                }
                if (version != loadVersion || !IsAdded || View == null) return;
                if (page == null || page.Items.Count == 0) continue;

                adapter.AddRow(new HomeRow { Title = request.Title, Style = request.Style, Items = page.Items, Endpoint = request.Endpoint, Query = request.Query });
                if (!anyRow)
                {
                    anyRow = true;
                    progress.Visibility = ViewStates.Gone;
                    BindHero(FirstWithArtwork(page));
                    if (isTv)
                    {
                        heroContainer.Visibility = ViewStates.Visible;
                        rows.Post(FocusFirstTile);
                    }
                }
            }

            progress.Visibility = ViewStates.Gone;
            if (!anyRow)
            {
                AppView.SetEmptyView(emptyView, true, false, () => _ = LoadAsync());
            }
        }

        MediaCard FirstWithArtwork(CardPage page)
        {
            foreach (var item in page.Items)
            {
                if (!string.IsNullOrEmpty(isTv ? item.BackdropPath : item.PosterPath)) return item;
            }
            return page.Items[0];
        }

        void FocusFirstTile()
        {
            var firstRow = rows?.GetChildAt(isTv ? 0 : 1);
            var list = firstRow?.FindViewById<RecyclerView>(Resource.Id.row_list);
            list?.GetChildAt(0)?.RequestFocus();
        }

        void OnCardFocused(MediaCard card, int rowPosition)
        {
            if (!isTv || card == null) return;

            // wait for focus to settle so scrolling quickly through a row doesn't thrash the hero
            focusDebounce?.Cancel();
            var cts = focusDebounce = new CancellationTokenSource();
            Task.Delay(220, cts.Token).ContinueWith(t =>
            {
                if (t.IsCanceled) return;
                Activity?.RunOnUiThread(() =>
                {
                    if (View != null && !cts.IsCancellationRequested) BindHero(card);
                });
            });
        }

        void OnViewAllClick(HomeRow row)
        {
            var intent = new Intent(Context, typeof(SeeAllActivity));
            intent.PutExtra("title", row.Title);
            intent.PutExtra("endpoint", row.Endpoint);
            intent.PutExtra("query", row.Query == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(row.Query));
            AppView.StartActivityWithTransition(Context, intent);
        }

        void BindHero(MediaCard card)
        {
            if (card == null || View == null) return;
            if (heroCard != null && heroCard.Id == card.Id) return;
            heroCard = card;

            if (isTv)
            {
                var url = card.Backdrop("w1280");
                AppView.LoadPicture(heroImage, string.IsNullOrEmpty(url) ? card.Poster("w780") : url);
                heroTitle.Text = card.Title;

                if (string.IsNullOrEmpty(card.Rating))
                {
                    tvHeroRating.Visibility = ViewStates.Gone;
                }
                else
                {
                    tvHeroRating.Text = card.Rating;
                    tvHeroRating.Visibility = ViewStates.Visible;
                }

                var metaParts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(card.Year)) metaParts.Add(card.Year);
                if (!string.IsNullOrEmpty(card.Genres)) metaParts.Add(card.Genres);
                var metaPrefix = tvHeroRating.Visibility == ViewStates.Visible ? "  •  " : string.Empty;
                heroMeta.Text = metaPrefix + string.Join("  •  ", metaParts);
                heroMeta.Visibility = metaParts.Count == 0 ? ViewStates.Gone : ViewStates.Visible;
                heroOverview.Text = card.Overview;
                heroOverview.Visibility = string.IsNullOrWhiteSpace(card.Overview) ? ViewStates.Gone : ViewStates.Visible;
            }
            else
            {
                adapter.SetHero(card, heroInList);
            }
            _ = RefreshHeroListStateAsync();
        }

        async Task RefreshHeroListStateAsync()
        {
            var card = heroCard;
            if (card == null) return;
            var inList = await StorageData.IsMarkedFavorite(new SeriesDetails { SeriesLink = card.PageLink });
            if (card != heroCard || View == null) return;
            ShowListState(inList);
        }

        void ShowListState(bool inList)
        {
            heroInList = inList;
            if (isTv)
            {
                heroList.Text = GetString(inList ? Resource.String.action_in_list : Resource.String.action_add_list);
                heroList.SetCompoundDrawablesRelativeWithIntrinsicBounds(inList ? Resource.Drawable.ic_check : Resource.Drawable.ic_add, 0, 0, 0);
            }
            else
            {
                adapter.SetHeroListState(inList);
            }
        }

        async Task ToggleHeroListAsync()
        {
            var card = heroCard;
            if (card == null) return;

            try
            {
                var details = new SeriesDetails { SeriesLink = card.PageLink };
                if (heroInList)
                {
                    if (await StorageData.RemoveSeriesFromFavoritesFile(details)) ShowListState(false);
                }
                else
                {
                    // the saved entry needs the full show data (artwork, status, next episode)
                    var full = await TMDbClient.GetDetailsForTVShowSeries(card.PageLink, includeEpisodes: false);
                    if (full != null && await StorageData.SaveSeriesToFavoritesFile(full)) ShowListState(true);
                    else Error.Instance.ShowErrorTip("Could not add to My List.", Context);
                }
            }
            catch (Exception)
            {
                Error.Instance.ShowErrorTip("My List could not be changed.", Context);
            }
        }

        /// <summary>Fades the fixed top bar's background in as the hero scrolls behind it (phone only).</summary>
        class TopBarFadeListener : RecyclerView.OnScrollListener
        {
            readonly HomeFragment owner;
            int accumulated;

            public TopBarFadeListener(HomeFragment owner)
            {
                this.owner = owner;
            }

            public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
            {
                accumulated = Math.Max(0, accumulated + dy);
                if (owner.topBarFadeDistancePx <= 0 || owner.topBarBackground == null) return;
                var ratio = Math.Min(1f, (float)accumulated / owner.topBarFadeDistancePx);
                owner.topBarBackground.Alpha = (int)(ratio * 255);
            }
        }
    }
}
