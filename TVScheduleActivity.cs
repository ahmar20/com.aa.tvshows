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
using AndroidX.Core.Content;
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
        AppCompatTextView status;
        LinearLayoutCompat emptyView;

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

            status = new AppCompatTextView(this);
            TextViewCompat.SetTextAppearance(status, Resource.Style.TextAppearance_AppCompat_Body2);
            status.TextAlignment = TextAlignment.Center;
            emptyView = FindViewById<LinearLayoutCompat>(Resource.Id.tv_schedule_empty_view);

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
            AppView.SetEmptyView(emptyView, false);
            if (viewPager.Adapter != null)
            {
                tabLayout.Visibility = viewPager.Visibility = ViewStates.Gone;
            }
            var tabsAdapter = new PageTabsAdapter(SupportFragmentManager);
            if (swipeRefresh != null)swipeRefresh.Refreshing = true;
            
            emptyView.Visibility = ViewStates.Visible;
            status.Text = "Loading...";
            emptyView.AddView(status);
            var data = await WebData.GetTVSchedule().ConfigureAwait(true);
            if (data != null && data.Count > 0)
            {
                AppView.SetEmptyView(emptyView, false);
                tabLayout.Visibility = ViewStates.Visible;
                viewPager.Visibility = ViewStates.Visible;
                viewPager.Adapter = tabsAdapter;
                
                foreach (var item in data)
                {
                    tabsAdapter.AddTab(new TitleFragment() { Title = item.Key, Fragmnet = new MainTabs(DataEnum.DataType.TVSchedule, new List<object>(item.Value)) });
                }
            }
            else
            {
                AppView.SetEmptyView(emptyView, true, false, delegate { SetupScheduleData(swipeRefresh); });
            }
            if (swipeRefresh != null) swipeRefresh.Refreshing = false;
        }
    }
}