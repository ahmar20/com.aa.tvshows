using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Widget;
using AndroidX.Interpolator.View.Animation;
using AndroidX.ViewPager.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Google.Android.Material.Tabs;
using Java.Lang;

namespace com.aa.tvshows
{
    public class TVScheduleActivity : RefreshableFragment
    {

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = LayoutInflater.Inflate(Resource.Layout.tv_schedule_base, container, false);
            SetUpTVScheduleTabs(view);

            return view;
        }



        public override void refresh()
        {
            if (View != null)
                SetUpTVScheduleTabs(View);
        }
        PageTabsAdapter tabsAdapter;
        private async void SetUpTVScheduleTabs(View v)
        {
            // load data and show tabs from dates
            // load each tab in a fragment and show relevant data
            var viewPager = v.FindViewById<ViewPager>(Resource.Id.main_tabs_viewpager);
            var tabLayout = v.FindViewById<TabLayout>(Resource.Id.main_tabs_header);

            tabLayout.Visibility = ViewStates.Invisible;
            tabLayout.TabMode = TabLayout.ModeScrollable;
            tabLayout.SetupWithViewPager(viewPager);

            var data = await WebData.GetTVSchedule().ConfigureAwait(true);
            if (tabsAdapter == null) tabsAdapter = new PageTabsAdapter(ChildFragmentManager);
            if (viewPager.Adapter == null) viewPager.Adapter = tabsAdapter;
            if (data != null && data.Count > 0)
            {
                tabLayout.Visibility = ViewStates.Visible;
                if (tabsAdapter.Fragments != null) tabsAdapter.Fragments.Clear();
                foreach (var item in data)
                {
                    tabsAdapter.AddTab(new TitleFragment() { Title = item.Key, Fragmnet = new MainTabs(DataEnum.DataType.TVSchedule, new List<object>(item.Value)) });
                }
            }
        }



    }
}