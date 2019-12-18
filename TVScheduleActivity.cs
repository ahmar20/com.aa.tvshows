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
using AndroidX.ViewPager.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Google.Android.Material.Tabs;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Schedule", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
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

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == AppView.ReloadId)
            {
                var action = new Action(() =>
                {
                    SetUpTVScheduleTabs();
                });
                return AppView.OnOptionsItemSelected(item, this, action);
            }
            return AppView.OnOptionsItemSelected(item, this);
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }

        private async void SetUpTVScheduleTabs()
        {
            // load data and show tabs from dates
            // load each tab in a fragment and show relevant data
            var viewPager = FindViewById<ViewPager>(Resource.Id.main_tabs_viewpager);
            var tabLayout = FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            var loadingView = FindViewById<ContentLoadingProgressBar>(Resource.Id.tv_schedule_loading);
            var emptyView = FindViewById<AppCompatTextView>(Resource.Id.tv_schedule_base_emptytext);

            tabLayout.Visibility = ViewStates.Invisible;
            emptyView.Visibility = ViewStates.Visible;
            loadingView.Visibility = ViewStates.Visible;

            tabLayout.TabMode = TabLayout.ModeScrollable;
            tabLayout.SetupWithViewPager(viewPager);
            emptyView.Text = "Loading...";

            var data = await WebData.GetTVSchedule().ConfigureAwait(true);
            var tabsAdapter = new PageTabsAdapter(SupportFragmentManager);
            viewPager.Adapter = tabsAdapter;
            if (data != null && data.Count > 0)
            {
                emptyView.Visibility = ViewStates.Gone;
                tabLayout.Visibility = ViewStates.Visible;
                foreach (var item in data)
                {
                    tabsAdapter.AddTab(new TitleFragment() { Title = item.Key, Fragmnet = new MainTabs(DataEnum.DataType.TVSchedule, item.Value) });
                }
            }
            emptyView.Text = Resources.GetString(Resource.String.empty_data_view);
            loadingView.Visibility = ViewStates.Gone;
        }
    }
}