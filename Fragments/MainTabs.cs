using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        readonly DataEnum.MainTabsType tabType = DataEnum.MainTabsType.None;
        readonly List<CalenderScheduleList> items;
        readonly DataEnum.GenreDataType genresType;
        readonly string genre;
        readonly int year;
        
        RecyclerView recyclerView;
        AppCompatTextView emptyView;
        ContentLoadingProgressBar loadingView;

        public MainTabs(DataEnum.MainTabsType tabType)
        {
            this.tabType = tabType;
        }

        public MainTabs(DataEnum.MainTabsType tabType, List<CalenderScheduleList> items) : this(tabType)
        {
            this.items = items;
        }

        public MainTabs(DataEnum.MainTabsType tabType, DataEnum.GenreDataType genresType, string genre, int year) : this(tabType)
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
            var layoutManager = new LinearLayoutManager(view.Context);
            recyclerView.SetLayoutManager(layoutManager);
            emptyView = view.FindViewById<AppCompatTextView>(Resource.Id.main_tab_emptytext);
            if (tabType != DataEnum.MainTabsType.TVSchedule)
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
            LoadDataForType();
        }

        private void LoadDataForType()
        {
            switch (tabType)
            {
                case DataEnum.MainTabsType.NewEpisodes:
                case DataEnum.MainTabsType.NewPopularEpisodes:
                case DataEnum.MainTabsType.PopularShows:
                    var mainAdapter = new EpisodesAdapter<EpisodeList>(tabType, emptyView, loadingView);
                    recyclerView.SetAdapter(mainAdapter);
                    mainAdapter.ItemClick += delegate
                    {
                        Android.Widget.Toast.MakeText(Activity, "Item clicked", Android.Widget.ToastLength.Short).Show();
                    };
                    break;

                case DataEnum.MainTabsType.TVSchedule:
                    var scheduleAdapter = new EpisodesAdapter<CalenderScheduleList>(items, tabType, emptyView);
                    recyclerView.SetAdapter(scheduleAdapter);
                    scheduleAdapter.ItemClick += delegate
                    {
                        Android.Widget.Toast.MakeText(Activity, "Item clicked", Android.Widget.ToastLength.Short).Show();
                    };
                    break;

                case DataEnum.MainTabsType.Genres:
                    if (genresType == DataEnum.GenreDataType.LatestEpisodes)
                    {
                        var genresAdapter = new EpisodesAdapter<GenresShow>(tabType, genresType, emptyView, loadingView);
                        recyclerView.SetAdapter(genresAdapter);
                        genresAdapter.ItemClick += delegate
                        {
                            Android.Widget.Toast.MakeText(Activity, "Item clicked", Android.Widget.ToastLength.Short).Show();
                        };
                    }
                    else if (genresType == DataEnum.GenreDataType.PopularEpisodes)
                    {
                        var genresAdapter = new EpisodesAdapter<GenresShow>(tabType, genresType, emptyView, loadingView);
                        recyclerView.SetAdapter(genresAdapter);
                        genresAdapter.ItemClick += delegate
                        {
                            Android.Widget.Toast.MakeText(Activity, "Item clicked", Android.Widget.ToastLength.Short).Show();
                        };
                    }
                    else if (genresType == DataEnum.GenreDataType.Shows)
                    {
                        var genresAdapter = new EpisodesAdapter<GenresShow>(tabType, genresType, genre, year, emptyView, loadingView);
                        recyclerView.SetAdapter(genresAdapter);
                        genresAdapter.ItemClick += delegate
                        {
                            Android.Widget.Toast.MakeText(Activity, "Item clicked", Android.Widget.ToastLength.Short).Show();
                        };
                    }
                    break;

                default:
                    break;
            }   
        }
    }
}