using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using com.aa.tvshows.Helper;

namespace com.aa.tvshows.Fragments
{
    public class MainTabs : Fragment
    {
        readonly DataEnum.DataType tabType = DataEnum.DataType.None;
        readonly List<object> items;
        readonly DataEnum.GenreDataType genresType;
        readonly string genre;
        readonly int year;
        int genrePage = 1;

        RecyclerView recyclerView;
        //AppCompatTextView emptyView;
        LinearLayoutCompat emptyView;
        SwipeRefreshLayout refreshView;
        LinearLayoutManager layoutManager;

        public MainTabs() : base()
        {
        }

        public MainTabs(DataEnum.DataType tabType) : this()
        {
            this.tabType = tabType;
        }

        public MainTabs(DataEnum.DataType tabType, IEnumerable<object> items) : this(tabType)
        {
            this.items = new List<object>(items);
        }

        public MainTabs(DataEnum.DataType tabType, DataEnum.GenreDataType genresType, string genre, int year) : this(tabType)
        {
            this.genresType = genresType;
            this.genre = genre;
            this.year = year;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            var view = LayoutInflater.Inflate(Resource.Layout.main_tab_content, container, false);
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.main_tab_rv);
            layoutManager = new CachingLayoutManager(view.Context);
            recyclerView.SetLayoutManager(layoutManager);
            emptyView = view.FindViewById<LinearLayoutCompat>(Resource.Id.main_tab_empty_view);
            refreshView = view.FindViewById<SwipeRefreshLayout>(Resource.Id.main_tab_content_refresh);
            refreshView.SetProgressBackgroundColorSchemeResource(Resource.Color.colorPrimaryDark);
            if (tabType == DataEnum.DataType.TVSchedule)
            {
                refreshView.Refresh += delegate { (Activity as TVScheduleActivity).SetupScheduleData(refreshView); };
            }
            else
            {
                refreshView.Refresh += (s, e) => { ReloadCurrentData(); };
            }
            //AnimHelper.FadeContents(view, true, false, null);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            ReloadCurrentData();
            //recyclerView.Post(new Action(() => { AnimHelper.FadeContents(view, false, true, null); }));
        }

        public void ReloadCurrentData()
        {
            genrePage = 1;
            LoadDataForType();
        }

        private async void LoadDataForType()
        {
            ShowLoadingView(refreshView, true);
            switch(tabType)
            {
                case DataEnum.DataType.PopularShows:
                    if (recyclerView.GetAdapter() is null)
                    {
                        if (await WebData.GetPopularShowsForMainView().ConfigureAwait(true) is List<ShowList> newShows)
                        {
                            SetEmptyView(false);
                            var adapter = new EpisodesAdapter<ShowList>(tabType, newShows, emptyView);
                            adapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), Activity);
                            };
                            recyclerView.SetAdapter(adapter);
                        }
                        else
                        {
                            SetEmptyView(true);
                        }
                    }
                    else
                    {
                        recyclerView.SetAdapter(null);
                        LoadDataForType();
                        return;
                    }
                    break;

                case DataEnum.DataType.NewPopularEpisodes:
                    if (recyclerView.GetAdapter() is null)
                    {
                        if (await WebData.GetPopularEpisodesForMainView().ConfigureAwait(true) is List<EpisodeList> popularEpisodes)
                        {
                            SetEmptyView(false);
                            var adapter = new EpisodesAdapter<EpisodeList>(tabType, popularEpisodes, emptyView);
                            adapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), Activity);
                            };
                            recyclerView.SetAdapter(adapter);
                        }
                        else
                        {
                            SetEmptyView(true);
                        }
                    }
                    else
                    {
                        recyclerView.SetAdapter(null);
                        LoadDataForType();
                        return;
                    }
                    break;

                case DataEnum.DataType.NewEpisodes:
                    if (recyclerView.GetAdapter() is null)
                    {
                        if (await WebData.GetNewestEpisodesForMainView().ConfigureAwait(true) is List<EpisodeList> newEpisodes)
                        {
                            SetEmptyView(false);
                            var adapter = new EpisodesAdapter<EpisodeList>(tabType, newEpisodes, emptyView);
                            adapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), Activity);
                            };
                            recyclerView.SetAdapter(adapter);
                        }
                        else
                        {
                            SetEmptyView(true);
                        }
                    }
                    else
                    {
                        recyclerView.SetAdapter(null);
                        LoadDataForType();
                        return;
                    }
                    break;

                case DataEnum.DataType.SeasonsEpisodes:
                    var seasonsEpisodesAdapter = new EpisodesAdapter<ShowEpisodeDetails>(tabType, new List<ShowEpisodeDetails>(items.Cast<ShowEpisodeDetails>()));
                    recyclerView.SetAdapter(seasonsEpisodesAdapter);
                    seasonsEpisodesAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(seasonsEpisodesAdapter.GetItem(e), Activity);
                    };
                    break;

                case DataEnum.DataType.TVSchedule:
                    var scheduleAdapter = new EpisodesAdapter<CalenderScheduleList>(tabType, new List<CalenderScheduleList>(items.Cast<CalenderScheduleList>()));
                    recyclerView.SetAdapter(scheduleAdapter);
                    scheduleAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(scheduleAdapter.GetItem(e), Activity);
                    };
                    break;

                case DataEnum.DataType.Genres:
                    EpisodesAdapter<GenresShow> genresAdapter = null;
                    /*
                    if (genresType == DataEnum.GenreDataType.LatestEpisodes)
                    {
                        genresAdapter = new EpisodesAdapter<GenresShow>(tabType, emptyView);
                    }
                    else if (genresType == DataEnum.GenreDataType.PopularEpisodes)
                    {
                        genresAdapter = new EpisodesAdapter<GenresShow>(tabType, emptyView);
                    }
                    else 
                    */
                    if (genresType == DataEnum.GenreDataType.Shows)
                    {
                        if (await WebData.GetGenresShows(genre, genrePage++, year).ConfigureAwait(true) is List<GenresShow> genresShows)
                        {
                            SetEmptyView(false);
                            genresAdapter = new EpisodesAdapter<GenresShow>(tabType, genresShows);
                            var scrollListener = new EndlessScroll(layoutManager);
                            scrollListener.LoadMoreTask += async delegate
                            {
                                refreshView.Refreshing = true;
                                var items = await WebData.GetGenresShows(genre, genrePage++, year);
                                if (items != null)
                                {
                                    genresAdapter.AddItem(items.ToArray());
                                }
                                refreshView.Refreshing = false;
                            };
                            recyclerView.AddOnScrollListener(scrollListener);
                            genresAdapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(genresAdapter.GetItem(e), Activity);
                            };
                        }
                        else
                        {
                            SetEmptyView(true);
                        }
                        recyclerView.SetAdapter(genresAdapter);
                    }
                    break;

                case DataEnum.DataType.UserFavorites:
                    if (recyclerView.GetAdapter() is null)
                    {
                        if (await StorageData.GetSeriesListFromFavoritesFile().ConfigureAwait(true) is List<SeriesDetails> userFavorites)
                        {
                            SetEmptyView(false);
                            var adapter = new EpisodesAdapter<SeriesDetails>(tabType, userFavorites, emptyView);
                            adapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), Activity);
                            };
                            recyclerView.SetAdapter(adapter);
                        }
                        else
                        {
                            SetEmptyView(true);
                        }
                    }
                    else
                    {
                        recyclerView.SetAdapter(null);
                        LoadDataForType();
                        return;
                    }
                    break;

                default:
                    break;
            }
            ShowLoadingView(refreshView, false);
        }

        private void SetEmptyView(bool show)
        {
            if (emptyView is null) return;

            emptyView.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
            emptyView.RemoveAllViews();
            if (!show)
            {
                return;
            }
            var parent = LayoutInflater.Inflate(Resource.Layout.empty_view, null, false);
            var emptyHeader = parent.FindViewById<AppCompatTextView>(Resource.Id.empty_view_header);
            var emptyContent = parent.FindViewById<AppCompatTextView>(Resource.Id.empty_view_content);
            var emptyImage = parent.FindViewById<AppCompatImageView>(Resource.Id.empty_view_tagline_image);
            var emptyRetryBtn = parent.FindViewById<AppCompatButton>(Resource.Id.empty_view_retry_btn);
            if (tabType == DataEnum.DataType.UserFavorites)
            {
                emptyHeader.Text = Resources.GetString(Resource.String.empty_favorites_header);
                emptyContent.Text = Resources.GetString(Resource.String.empty_favorites_content);
                emptyImage.SetImageDrawable(ContextCompat.GetDrawable(parent.Context, Resource.Drawable.baseline_favorite_24));
                emptyRetryBtn.Click += delegate { ReloadCurrentData(); };
            }
            else
            {
                emptyHeader.Text = Resources.GetString(Resource.String.internet_error_header);
                emptyContent.Text = Resources.GetString(Resource.String.internet_error_content);
                emptyImage.SetImageDrawable(ContextCompat.GetDrawable(parent.Context, Resource.Drawable.sharp_error_outline_24));
                emptyRetryBtn.Click += delegate { ReloadCurrentData(); };
            }
            AnimHelper.SetAlphaAnimation(emptyImage);
            emptyView.AddView(parent);
        }

        private void ShowLoadingView(View view, bool show)
        {
            if (view is null) return;
            if (view is SwipeRefreshLayout swipe)
            {
                if (swipe.Refreshing && show) return;
                swipe.Enabled = !show;
                swipe.Refreshing = show;
            }
            else if (view is ContentLoadingProgressBar prog)
            {
                prog.Indeterminate = true;
                prog.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
            }
        }
    }
}