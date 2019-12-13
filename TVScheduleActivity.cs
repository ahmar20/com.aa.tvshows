using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
//using AndroidX.AppCompat.App;
//using AndroidX.AppCompat.Widget;
//using AndroidX.ViewPager.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
//using Google.Android.Material.Tabs;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Schedule")]
    public class TVScheduleActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.tv_schedule_base);
            var toolbar = FindViewById<Toolbar>(Resource.Id.main_toolbar);
            AppView.SetActionBarForActivity(toolbar, this);

            SetUpTVScheduleTabs();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return AppView.ShowOptionsMenu(menu, this);
        }

        private async void SetUpTVScheduleTabs()
        {
            // load data and show tabs from dates
            // load each tab in a fragment and show relevant data
            var viewPager = FindViewById<ViewPager>(Resource.Id.main_tabs_viewpager);
            var tabLayout = FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            tabLayout.Visibility = ViewStates.Gone;
            tabLayout.TabMode = TabLayout.ModeScrollable;
            tabLayout.SetupWithViewPager(viewPager);
            var data = await Web.GetTVSchedule().ConfigureAwait(true);
            var tabsAdapter = new PageTabsAdapter(SupportFragmentManager);
            viewPager.Adapter = tabsAdapter;
            if (data != null)
            {
                foreach (var item in data)
                {
                    tabsAdapter.AddTab(new TitleFragment() { Title = item.Key, Fragmnet = new MainTabs(DataEnum.MainTabsType.TVSchedule, item.Value) });
                }
            }
            tabLayout.Visibility = ViewStates.Visible;
        }
    }
}