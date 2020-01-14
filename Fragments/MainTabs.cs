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
using AndroidX.Core.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
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
        AppCompatTextView emptyView;
        ContentLoadingProgressBar loadingView;
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
            recyclerView.ClearOnScrollListeners();
            emptyView = view.FindViewById<AppCompatTextView>(Resource.Id.main_tab_emptytext);
            if (tabType != DataEnum.DataType.TVSchedule)
            {
                loadingView = view.FindViewById<ContentLoadingProgressBar>(Resource.Id.main_tab_loading);
            }
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            LoadDataForType();
        }

        public void ReloadCurrentData()
        {
            genrePage = 1;
            LoadDataForType();
        }

        private async void LoadDataForType()
        {
            switch (tabType)
            {
                case DataEnum.DataType.NewEpisodes:
                case DataEnum.DataType.NewPopularEpisodes:
                case DataEnum.DataType.PopularShows:
                    var mainAdapter = new EpisodesAdapter<EpisodeList>(tabType, emptyView, loadingView);
                    recyclerView.SetAdapter(mainAdapter);
                    mainAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(mainAdapter.GetItem(e), Activity);
                    };
                    break;

                case DataEnum.DataType.TVSchedule:
                    var scheduleAdapter = new EpisodesAdapter<CalenderScheduleList>(new List<CalenderScheduleList>(items.Cast<CalenderScheduleList>()), tabType, emptyView);
                    recyclerView.SetAdapter(scheduleAdapter);
                    scheduleAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(scheduleAdapter.GetItem(e), Activity);
                    };
                    break;

                case DataEnum.DataType.Genres:
                    EpisodesAdapter<GenresShow> genresAdapter = null;
                    if (genresType == DataEnum.GenreDataType.LatestEpisodes)
                    {
                        genresAdapter = new EpisodesAdapter<GenresShow>(tabType, emptyView, loadingView);
                    }
                    else if (genresType == DataEnum.GenreDataType.PopularEpisodes)
                    {
                        genresAdapter = new EpisodesAdapter<GenresShow>(tabType, emptyView, loadingView);
                    }
                    else if (genresType == DataEnum.GenreDataType.Shows)
                    {
                        loadingView.Visibility = ViewStates.Visible;
                        var genreList = await WebData.GetGenresShows(genre, genrePage++, year);
                        loadingView.Visibility = ViewStates.Invisible;
                        genresAdapter = new EpisodesAdapter<GenresShow>(genreList, tabType, emptyView);
                        var scrollListener = new EndlessScroll(layoutManager);
                        scrollListener.LoadMoreTask += async delegate
                        {
                            loadingView.Visibility = ViewStates.Visible;
                            var items = await WebData.GetGenresShows(genre, genrePage++, year);
                            if (items != null)
                            {
                                genresAdapter.AddItem(items.ToArray());
                            }
                            loadingView.Visibility = ViewStates.Invisible;
                        };
                        recyclerView.AddOnScrollListener(scrollListener);
                    }
                    recyclerView.SetAdapter(genresAdapter);
                    genresAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(genresAdapter.GetItem(e), Activity);
                    };
                    break;

                case DataEnum.DataType.SeasonsEpisodes:
                    var seasonsEpisodesAdapter = new EpisodesAdapter<ShowEpisodeDetails>(new List<ShowEpisodeDetails>(items.Cast<ShowEpisodeDetails>()), tabType, emptyView);
                    recyclerView.SetAdapter(seasonsEpisodesAdapter);
                    seasonsEpisodesAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(seasonsEpisodesAdapter.GetItem(e), Activity);
                    };
                    break;

                case DataEnum.DataType.UserFavorites:
                    var favsAdapter = new EpisodesAdapter<SeriesDetails>(tabType, emptyView, loadingView);
                    recyclerView.SetAdapter(favsAdapter);
                    favsAdapter.ItemClick += (s, e) =>
                    {
                        AppView.HandleItemShowEpisodeClick(favsAdapter.GetItem(e), Activity);
                    };
                    break;

                default:
                    break;
            }
        }
    }
}