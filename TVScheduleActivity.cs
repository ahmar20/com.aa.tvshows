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
using AndroidX.SwipeRefreshLayout.Widget;
using AndroidX.ViewPager.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Google.Android.Material.Tabs;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Schedule", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class TVScheduleActivity : AppCompatActivity
    {
        ViewPager viewPager;
        TabLayout tabLayout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.tv_schedule_base);
            var toolbar = FindViewById<Toolbar>(Resource.Id.main_toolbar);
            AppView.SetActionBarForActivity(toolbar, this);

            viewPager = FindViewById<ViewPager>(Resource.Id.main_tabs_viewpager);
            tabLayout = FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            tabLayout.Visibility = ViewStates.Invisible;
            tabLayout.TabMode = TabLayout.ModeScrollable;
            tabLayout.SetupWithViewPager(viewPager);

            SetupScheduleData();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return AppView.ShowOptionsMenu(menu, this);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return AppView.OnOptionsItemSelected(item, this);
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }

        public async void SetupScheduleData(SwipeRefreshLayout swipeRefresh = default)
        {
            if (viewPager.Adapter != null)
            {
                tabLayout.Visibility = ViewStates.Gone;
                viewPager.Adapter = null;
            }
            var emptyView = FindViewById<AppCompatTextView>(Resource.Id.tv_schedule_base_emptytext);
            var tabsAdapter = new PageTabsAdapter(SupportFragmentManager);
            emptyView.Visibility = ViewStates.Visible;
            emptyView.Text = "Loading...";
            if (swipeRefresh != null)swipeRefresh.Refreshing = true;

            var data = await WebData.GetTVSchedule().ConfigureAwait(true);
            viewPager.Adapter = tabsAdapter;
            if (data != null && data.Count > 0)
            {
                emptyView.Visibility = ViewStates.Gone;
                tabLayout.Visibility = ViewStates.Visible;
                foreach (var item in data)
                {
                    tabsAdapter.AddTab(new TitleFragment() { Title = item.Key, Fragmnet = new MainTabs(DataEnum.DataType.TVSchedule, new List<object>(item.Value)) });
                }
            }
            emptyView.Text = Resources.GetString(Resource.String.empty_data_view);
            if (swipeRefresh != null) swipeRefresh.Refreshing = false;
        }
    }
}