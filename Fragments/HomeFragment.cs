using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Fragments
{
    /// <summary>
    /// Netflix-style home: a hero for the featured show above horizontal rows.
    /// Phone: the hero scrolls away with the page. TV: the hero is a fixed backdrop that follows
    /// whichever tile has focus.
    /// </summary>
    public class HomeFragment : Fragment
    {
        bool isTv;
        ImageView heroImage;
        TextView heroTitle, heroMeta, heroOverview, heroDetails, heroList;
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

            heroImage = view.FindViewById<ImageView>(Resource.Id.hero_image);
            heroTitle = view.FindViewById<TextView>(Resource.Id.hero_title);
            heroMeta = view.FindViewById<TextView>(Resource.Id.hero_meta);
            heroOverview = view.FindViewById<TextView>(Resource.Id.hero_overview);
            heroDetails = view.FindViewById<TextView>(Resource.Id.hero_btn_details);
            heroList = view.FindViewById<TextView>(Resource.Id.hero_btn_list);
            rows = view.FindViewById<RecyclerView>(Resource.Id.home_rows);
            progress = view.FindViewById<ProgressBar>(Resource.Id.home_progress);
            emptyView = view.FindViewById<LinearLayoutCompat>(Resource.Id.home_empty_view);

            // phone: the wordmark / Genres / Settings bar floats over the hero, below the status bar
            var topBar = view.FindViewById<View>(Resource.Id.hero_top_bar);
            if (topBar != null)
            {
                WindowInsetsHelper.Apply(topBar, top: true);
                view.FindViewById<View>(Resource.Id.hero_genres_btn).Click += (s, e) => StartActivity(new Intent(Context, typeof(GenresActivity)));
                view.FindViewById<View>(Resource.Id.hero_settings_btn).Click += (s, e) => StartActivity(new Intent(Context, typeof(SettingsActivity)));
            }

            heroDetails.Click += (s, e) =>
            {
                if (heroCard != null) AppView.HandleItemShowEpisodeClick(heroCard, Context);
            };
            heroList.Click += async (s, e) => await ToggleHeroListAsync();

            rows.SetLayoutManager(new LinearLayoutManager(Context));
            adapter = new HomeRowsAdapter(isTv);
            adapter.CardClick += card => AppView.HandleItemShowEpisodeClick(card, Context);
            adapter.CardFocused += OnCardFocused;
            rows.SetAdapter(adapter);

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

                adapter.AddRow(new HomeRow { Title = request.Title, Style = request.Style, Items = page.Items });
                if (!anyRow)
                {
                    anyRow = true;
                    progress.Visibility = ViewStates.Gone;
                    BindHero(FirstWithArtwork(page));
                    if (isTv) rows.Post(FocusFirstTile);
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
            var firstRow = rows?.GetChildAt(0);
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

        void BindHero(MediaCard card)
        {
            if (card == null || View == null) return;
            if (heroCard != null && heroCard.Id == card.Id && heroTitle.Text == card.Title) return;
            heroCard = card;

            var url = isTv ? card.Backdrop("w1280") : card.Poster("w780");
            AppView.LoadPicture(heroImage, string.IsNullOrEmpty(url) ? card.Poster("w780") : url);

            heroTitle.Text = card.Title;
            heroMeta.Text = card.MetaLine;
            heroMeta.Visibility = string.IsNullOrEmpty(heroMeta.Text) ? ViewStates.Gone : ViewStates.Visible;
            heroOverview.Text = card.Overview;
            heroOverview.Visibility = string.IsNullOrWhiteSpace(card.Overview) ? ViewStates.Gone : ViewStates.Visible;
            _ = RefreshHeroListStateAsync();
        }

        async Task RefreshHeroListStateAsync()
        {
            var card = heroCard;
            if (card == null || heroList == null) return;
            var inList = await StorageData.IsMarkedFavorite(new SeriesDetails { SeriesLink = card.PageLink });
            if (card != heroCard || View == null) return;
            ShowListState(inList);
        }

        void ShowListState(bool inList)
        {
            heroInList = inList;
            heroList.Text = GetString(inList ? Resource.String.action_in_list : Resource.String.action_add_list);
            heroList.SetCompoundDrawablesRelativeWithIntrinsicBounds(inList ? Resource.Drawable.ic_check : Resource.Drawable.ic_add, 0, 0, 0);
        }

        async Task ToggleHeroListAsync()
        {
            var card = heroCard;
            if (card == null) return;

            heroList.Enabled = false;
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
            finally
            {
                if (heroList != null) heroList.Enabled = true;
            }
        }
    }
}
