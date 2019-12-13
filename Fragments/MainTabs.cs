using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
//using AndroidX.Fragment.App;
//using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;

namespace com.aa.tvshows.Fragments
{
    public class MainTabs : Fragment
    {
        readonly DataEnum.MainTabsType tabType = DataEnum.MainTabsType.None;
        readonly List<CalenderScheduleList> items;
        RecyclerView recyclerView;
        TextView emptyView;
        ProgressBar loadingView;

        public MainTabs(DataEnum.MainTabsType tabType)
        {
            this.tabType = tabType;
        }

        public MainTabs(DataEnum.MainTabsType tabType, List<CalenderScheduleList> items) : this(tabType)
        {
            this.items = items;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (inflater == null) throw new ArgumentNullException(nameof(inflater));
            // Use this to return your custom view for this Fragment
            if (tabType == DataEnum.MainTabsType.NewPopularEpisodes)
            {
                return inflater.Inflate(Resource.Layout.main_popular_episodes_tab, container, false);
            }
            else if (tabType == DataEnum.MainTabsType.PopularShows)
            {
                return inflater.Inflate(Resource.Layout.main_popular_shows_tab, container, false);
            }
            else if (tabType == DataEnum.MainTabsType.NewEpisodes)
            {
                return inflater.Inflate(Resource.Layout.main_newest_episodes_tab, container, false);
            }
            else if (tabType == DataEnum.MainTabsType.TVSchedule)
            {
                return inflater.Inflate(Resource.Layout.tv_schedule_list, container, false);
            }
            return null;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            if (tabType == DataEnum.MainTabsType.PopularShows)
            {
                recyclerView = Activity.FindViewById<RecyclerView>(Resource.Id.main_popular_shows_rv);
                emptyView = Activity.FindViewById<TextView>(Resource.Id.main_popular_shows_emptytext);
                emptyView.Visibility = ViewStates.Gone;
                loadingView = Activity.FindViewById<ProgressBar>(Resource.Id.main_popular_shows_loading);
                loadingView.Visibility = ViewStates.Gone;
            }
            else if (tabType == DataEnum.MainTabsType.NewPopularEpisodes)
            {
                recyclerView = Activity.FindViewById<RecyclerView>(Resource.Id.main_popular_episodes_rv);
                emptyView = Activity.FindViewById<TextView>(Resource.Id.main_popular_episodes_emptytext);
                emptyView.Visibility = ViewStates.Gone;
                loadingView = Activity.FindViewById<ProgressBar>(Resource.Id.main_popular_episodes_loading);
                loadingView.Visibility = ViewStates.Gone;
            }
            else if (tabType == DataEnum.MainTabsType.NewEpisodes)
            {
                recyclerView = Activity.FindViewById<RecyclerView>(Resource.Id.main_newest_episodes_rv);
                emptyView = Activity.FindViewById<TextView>(Resource.Id.main_newest_episodes_emptytext);
                emptyView.Visibility = ViewStates.Gone;
                loadingView = Activity.FindViewById<ProgressBar>(Resource.Id.main_newest_episodes_loading);
                loadingView.Visibility = ViewStates.Gone;
            }
            else if (tabType == DataEnum.MainTabsType.TVSchedule)
            {
                recyclerView = Activity.FindViewById<RecyclerView>(Resource.Id.tv_schedule_rv);
                emptyView = Activity.FindViewById<TextView>(Resource.Id.tv_schedule_emptytext);
                emptyView.Visibility = ViewStates.Gone;
            }
            var layoutManager = new LinearLayoutManager(Activity);
            recyclerView.SetLayoutManager(layoutManager);
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
                        Toast.MakeText(Activity, "Item clicked", ToastLength.Short).Show();
                    };
                    break;

                case DataEnum.MainTabsType.TVSchedule:
                    var scheduleAdapter = new EpisodesAdapter<CalenderScheduleList>(items, tabType, emptyView);
                    recyclerView.SetAdapter(scheduleAdapter);
                    scheduleAdapter.ItemClick += delegate
                    {
                        Toast.MakeText(Activity, "Item clicked", ToastLength.Short).Show();
                    };
                    break;

                default:
                    break;
            }
            
        }
    }
}