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
using Google.Android.Material.BottomNavigation;
using static Google.Android.Material.BottomNavigation.BottomNavigationView;
using AndroidX.SwipeRefreshLayout.Widget;
using static AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout;
using com.aa.tvshows.Fragments;
using Java.Lang;
using AndroidX.Interpolator.View.Animation;

namespace com.aa.tvshows
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MainActivity : AppCompatActivity, IOnRefreshListener, BottomNavigationView.IOnNavigationItemSelectedListener
    {

        BottomNavigationView bottomNavigation;
        SwipeRefreshLayout swipeRefreshLayout;
        View contents;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            swipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipe_refresh);
            swipeRefreshLayout.SetProgressBackgroundColor(Resource.Color.colorAccent);
            swipeRefreshLayout.SetColorSchemeResources(Resource.Color.card);
            contents = FindViewById<View>(Resource.Id.contents);
            swipeRefreshLayout.SetOnRefreshListener(this);
            bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottom_navigation);
            bottomNavigation.SetOnNavigationItemSelectedListener(this);
            bottomNavigation.SelectedItemId = Resource.Id.navigation_home;


            //SetupSearchView();
        }

        public void toggleLoader(bool toggle)
        {
            if (swipeRefreshLayout != null) swipeRefreshLayout.Refreshing = toggle;

        }
        public void openFragment(AndroidX.Fragment.App.Fragment fragment)
        {
            if (fragment == null) return;
            toggleLoader(true);
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contents, fragment, "current_fragmnet").Commit();
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                default:
                case Resource.Id.navigation_home:
                    openFragment(new Fragments.MainTabs(DataEnum.DataType.Home));
                    return true;
                case Resource.Id.navigation_fav:
                    openFragment(new Fragments.MainTabs(DataEnum.DataType.UserFavorites));
                    return true;
                case Resource.Id.navigation_schedule:
                    openFragment(new TVScheduleActivity());
                    return true;
                case Resource.Id.navigation_genres:
                    openFragment(new GenresActivity());
                    return true;
                case Resource.Id.navigation_search:
                    StartActivity(typeof(SearchActivity));
                    return false;



            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            //Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return AppView.ShowOptionsMenu(menu, this);
        }



        public void OnRefresh()
        {
            var fragment = SupportFragmentManager.FindFragmentByTag("current_fragmnet");
            if (fragment is RefreshableFragment frag)
            {
                frag?.refresh();
            }

            swipeRefreshLayout.Refreshing = false;

        }


    }
}