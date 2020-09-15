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
using Newtonsoft.Json;

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
        LinearLayoutCompat emptyView;
        SwipeRefreshLayout refreshView;
        LinearLayoutManager layoutManager;

        public MainTabs() : base()
        {
        }

        /*public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            if (outState != null)
                outState.PutString(tabType.ToString(), JsonConvert.SerializeObject(recyclerView.GetAdapter()));
        }

        public override void OnViewStateRestored(Bundle savedInstanceState)
        {
            base.OnViewStateRestored(savedInstanceState);
            if (savedInstanceState != null)
                recyclerView.SetAdapter(JsonConvert.DeserializeObject<RecyclerView.Adapter>(savedInstanceState.GetString(tabType.ToString())));
        }*/

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
            var view = inflater.Inflate(Resource.Layout.main_tab_content, container, false);
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
            AppView.ShowLoadingView(refreshView, true);
            switch (tabType)
            {
                case DataEnum.DataType.PopularShows:
                    if (recyclerView.GetAdapter() is null)
                    {
                        if (await WebData.GetPopularShowsForMainView().ConfigureAwait(true) is List<ShowList> newShows)
                        {
                            AppView.SetEmptyView(emptyView, false, false, delegate { ReloadCurrentData(); });
                            var adapter = new EpisodesAdapter<ShowList>(newShows, emptyView);
                            adapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), Activity);
                            };
                            recyclerView.SetAdapter(adapter);
                            recyclerView.ClearOnScrollListeners();
                            var endlessScroll = new EndlessScroll(layoutManager);
                            endlessScroll.LoadMoreTask += async (s, e) =>
                            {
                                AppView.ShowLoadingView(refreshView, true);
                                ShowList lastItem = adapter.GetItem(adapter.ItemCount - 1);
                                int nextPageNumber = lastItem.NextPageNumber;
                                var newItems = await WebData.GetPopularShowsForMainView(nextPageNumber);
                                if (newItems != null)
                                {
                                    AppView.ShowLoadingView(refreshView, false);
                                    if (newItems.Where(a => a.PageLink == lastItem.PageLink) is ShowList duplicate)
                                    {
#if DEBUG
                                        Error.Instance.ShowErrorTip("End of shows reached", this.Context);
#endif
                                        return;
                                    }
                                    adapter.AddItem(newItems.ToArray());
                                }
                            };
                            recyclerView.AddOnScrollListener(endlessScroll);
                        }
                        else
                        {
                            AppView.SetEmptyView(emptyView, true, false, delegate { ReloadCurrentData(); });
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
                case DataEnum.DataType.NewEpisodes:
                    if (recyclerView.GetAdapter() is null)
                    {
                        if (await WebData.GetEpisodesForMainView(tabType).ConfigureAwait(true) is List<EpisodeList> popularEpisodes)
                        {
                            AppView.SetEmptyView(emptyView, false, false, delegate { ReloadCurrentData(); });
                            var adapter = new EpisodesAdapter<EpisodeList>(popularEpisodes, emptyView);
                            adapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), Activity);
                            };
                            recyclerView.SetAdapter(adapter);
                            recyclerView.ClearOnScrollListeners();
                            var endlessScroll = new EndlessScroll(layoutManager);
                            endlessScroll.LoadMoreTask += async (s, e) =>
                            {
                                AppView.ShowLoadingView(refreshView, true);
                                EpisodeList lastItem = adapter.GetItem(adapter.ItemCount - 1);
                                int nextPageNumber = lastItem.NextPageNumber;
                                var newItems = await WebData.GetEpisodesForMainView(tabType, nextPageNumber);
                                if (newItems != null)
                                {
                                    AppView.ShowLoadingView(refreshView, false);
                                    if (newItems.Where(a => a.PageLink == lastItem.PageLink) is ShowList duplicate)
                                    {
#if DEBUG
                                        Error.Instance.ShowErrorTip("End of shows reached", this.Context);
#endif
                                        return;
                                    }
                                    adapter.AddItem(newItems.ToArray());
                                }
                            };
                            recyclerView.AddOnScrollListener(endlessScroll);
                        }
                        else
                        {
                            AppView.SetEmptyView(emptyView, true, false, delegate { ReloadCurrentData(); });
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
                    var seasonsEpisodesAdapter = new EpisodesAdapter<ShowEpisodeDetails>(new List<ShowEpisodeDetails>(items.Cast<ShowEpisodeDetails>()));
                    recyclerView.SetAdapter(seasonsEpisodesAdapter);
                    seasonsEpisodesAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(seasonsEpisodesAdapter.GetItem(e), Activity);
                    };
                    break;

                case DataEnum.DataType.TVSchedule:
                    var scheduleAdapter = new EpisodesAdapter<CalenderScheduleList>(new List<CalenderScheduleList>(items.Cast<CalenderScheduleList>()));
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
                        if (!(recyclerView.GetAdapter() is null)) recyclerView.SetAdapter(null);
                        if (await WebData.GetGenresShows(genre, genrePage++, year).ConfigureAwait(true) is List<GenresShow> genresShows)
                        {
                            AppView.SetEmptyView(emptyView, false, false, delegate { ReloadCurrentData(); });
                            genresAdapter = new EpisodesAdapter<GenresShow>(genresShows);
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
                            AppView.SetEmptyView(emptyView, true, false, delegate { ReloadCurrentData(); });
                        }
                        recyclerView.SetAdapter(genresAdapter);
                    }
                    break;

                case DataEnum.DataType.UserFavorites:
                    if (recyclerView.GetAdapter() is null)
                    {
                        if (await StorageData.GetSeriesListFromFavoritesFile().ConfigureAwait(true) is List<SeriesDetails> userFavorites)
                        {
                            AppView.SetEmptyView(emptyView, false, true, delegate { ReloadCurrentData(); });
                            var adapter = new EpisodesAdapter<SeriesDetails>(userFavorites, emptyView);
                            adapter.ItemClick += (s, e) =>
                            {
                                AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), Activity);
                            };
                            recyclerView.SetAdapter(adapter);
                        }
                        else
                        {
                            AppView.SetEmptyView(emptyView, true, true, delegate { ReloadCurrentData(); });
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
            AppView.ShowLoadingView(refreshView, false);
        }
    }
}