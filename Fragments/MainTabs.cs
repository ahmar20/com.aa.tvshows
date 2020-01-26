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
using AndroidX.Interpolator.View.Animation;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using Java.Lang;

namespace com.aa.tvshows.Fragments
{
    public class MainTabs : RefreshableFragment
    {
        readonly DataEnum.DataType tabType = DataEnum.DataType.None;
        readonly List<object> items;
        readonly DataEnum.GenreDataType genresType;
        readonly string genre;
        readonly int year;
        int genrePage = 1;

        RecyclerView recyclerView;
        AppCompatTextView emptyView;
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

            if (tabType == DataEnum.DataType.UserFavorites || tabType == DataEnum.DataType.Home)
                layoutManager = new CachingGridLayoutManager(view.Context);
            else layoutManager = new CachingLayoutManager(view.Context);
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.ClearOnScrollListeners();
            emptyView = view.FindViewById<AppCompatTextView>(Resource.Id.main_tab_emptytext);

            AnimHelper.FadeContents(view, true, false, null);

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            LoadDataForType();
            recyclerView.Post(new Java.Lang.Runnable(() => { AnimHelper.FadeContents(View, false, true, new Java.Lang.Runnable(() => { toggleLoader(false); })); }));

        }

        public override void refresh()
        {
            ReloadCurrentData();
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
                case DataEnum.DataType.Home:
                case DataEnum.DataType.NewEpisodes:
                case DataEnum.DataType.NewPopularEpisodes:
                case DataEnum.DataType.PopularShows:
                    var mainAdapter = new EpisodesAdapter<EpisodeList>(DataEnum.DataType.Home, emptyView);
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
                        genresAdapter = new EpisodesAdapter<GenresShow>(tabType, emptyView);
                    }
                    else if (genresType == DataEnum.GenreDataType.PopularEpisodes)
                    {
                        genresAdapter = new EpisodesAdapter<GenresShow>(tabType, emptyView);
                    }
                    else if (genresType == DataEnum.GenreDataType.Shows)
                    {
                        toggleLoader(true);
                        var genreList = await WebData.GetGenresShows(genre, genrePage++, year);
                        genresAdapter = new EpisodesAdapter<GenresShow>(genreList, tabType, emptyView);
                        var scrollListener = new EndlessScroll(layoutManager);
                        scrollListener.LoadMoreTask += async delegate
                        {
                            toggleLoader(true);
                            var items = await WebData.GetGenresShows(genre, genrePage++, year);
                            if (items != null)
                            {
                                genresAdapter.AddItem(items.ToArray());
                            }
                            toggleLoader(false);
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
                    var favsAdapter = new EpisodesAdapter<SeriesDetails>(tabType, emptyView);
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



        Handler handler = new Android.OS.Handler();

        private void toggleLoader(bool toggle)
        {
            if (View == null || Activity == null) return;

            if (Activity is MainActivity x)
            {
                if (toggle) x.toggleLoader(toggle);
                else
                {
                    handler.PostDelayed(new Java.Lang.Runnable(() => { x.toggleLoader(toggle); }), 2000);
                }
            }
        }
    }
}