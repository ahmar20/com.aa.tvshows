using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Runtime;
using AndroidX.ViewPager.Widget;
using Google.Android.Material.Tabs;
using com.aa.tvshows.Helper;
using AndroidX.AppCompat.Widget;
using Android.Views;
using System;
using AndroidX.CursorAdapter.Widget;
using Android.Database;
using AndroidX.ViewPager2.Widget;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Collections.Generic;

namespace com.aa.tvshows
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true, 
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MainActivity : AppCompatActivity
    {
        ViewPager2 viewPager;
        TabLayout tabLayout;
        PageTabsAdapter tabsAdapter;
        /*
        const string PopularShowsFragmentKey = "popularShows";
        const string NewEpisodesFragmentKey = "newEpisodes";
        const string FavoritesFragmentKey = "favorites";
        */

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.main_toolbar);
            AppView.SetActionBarForActivity(toolbar, this);

            viewPager = FindViewById<ViewPager2>(Resource.Id.main_tabs_viewpager);
            viewPager.OffscreenPageLimit = 3;
            tabLayout = FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            SetupTabs();
            //SetupSearchView();
            AppCenter.Start("94d2c36e-82e2-4561-91d6-85544e9cbdb8", typeof(Analytics), typeof(Crashes));
        }

        /*protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            SupportFragmentManager.PutFragment(outState, PopularShowsFragmentKey, tabsAdapter.GetTab(0).Fragmnet);
            SupportFragmentManager.PutFragment(outState, NewEpisodesFragmentKey, tabsAdapter.GetTab(1).Fragmnet);
            SupportFragmentManager.PutFragment(outState, FavoritesFragmentKey, tabsAdapter.GetTab(2).Fragmnet);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            var fragments = new List<TitleFragment>()
            {
                new TitleFragment() { Title = "Popular Shows", Fragmnet = SupportFragmentManager.GetFragment(savedInstanceState, PopularShowsFragmentKey) },
                new TitleFragment() { Title = "New Episodes", Fragmnet = SupportFragmentManager.GetFragment(savedInstanceState, NewEpisodesFragmentKey) },
                new TitleFragment() { Title = "Your Favorites", Fragmnet = SupportFragmentManager.GetFragment(savedInstanceState, FavoritesFragmentKey) }
            };
            base.OnRestoreInstanceState(savedInstanceState);
        }*/

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            //Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return AppView.ShowOptionsMenu(menu, this);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return AppView.OnOptionsItemSelected(item, this);
        }

        public void SetupTabs(List<TitleFragment> savedFragments = default)
        {
            PageTabsAdapter tabsAdapter = new PageTabsAdapter(this);
            /*
            // not really needed because there is not much difference between
            // popular episodes and new episodes and also new episodes are more
            // than popular ones
            tabsAdapter.AddTab(new TitleFragment()
            {
                Fragmnet = new Fragments.MainTabs(DataEnum.MainTabsType.PopularEpisodes),
                Title = "Popular Episodes"
            });
            */
            tabsAdapter.AddTab(new TitleFragment()
            {
                Fragmnet = new Fragments.MainTabs(DataEnum.DataType.PopularShows),
                Title = "Popular Shows"
            });
            tabsAdapter.AddTab(new TitleFragment()
            {
                Fragmnet = new Fragments.MainTabs(DataEnum.DataType.NewEpisodes),
                Title = "New Episodes"
            });
            tabsAdapter.AddTab(new TitleFragment()
            {
                Fragmnet = new Fragments.MainTabs(DataEnum.DataType.UserFavorites),
                Title = "Your Favorites"
            });
            viewPager.Adapter = tabsAdapter;
            TabMediatorStrategy tabStrategy = new TabMediatorStrategy(tabsAdapter.Fragments);
            TabLayoutMediator tabMediator = new TabLayoutMediator(tabLayout, viewPager, tabStrategy);
            tabMediator.Attach();
        }
    }
}