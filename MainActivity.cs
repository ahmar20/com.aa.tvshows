using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Activity;
using AndroidX.Fragment.App;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;

namespace com.aa.tvshows
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { "android.intent.category.LEANBACK_LAUNCHER" })]
    public class MainActivity : AppCompatActivity
    {
        // The four top-level destinations. Phone: bottom bar. TV: left rail (which also has Genres and Settings).
        static readonly string[] TabTags = { "home", "search", "schedule", "list" };
        const string SelectedTabKey = "selectedTab";

        TextView[] navItems;
        int selectedTab = -1;
        ActionBackCallback backCallback;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            TMDbClient.ApiKey = Secrets.TMDB_Api_Key;
            MetadataScheduler.ScheduleDailyRefresh(ApplicationContext);
            _ = MetadataCache.RefreshIfStaleAsync(ApplicationContext);

            WindowInsetsHelper.EnableEdgeToEdge(this);
            SetContentView(Resource.Layout.activity_main);

            var nav = FindViewById<View>(Resource.Id.main_nav);
            // the bar sits on the screen edge: keep its icons clear of the gesture / button area
            WindowInsetsHelper.Apply(nav, bottom: !UiMode.IsTv(this), sides: true);

            navItems = new[]
            {
                FindViewById<TextView>(Resource.Id.nav_home),
                FindViewById<TextView>(Resource.Id.nav_search),
                FindViewById<TextView>(Resource.Id.nav_schedule),
                FindViewById<TextView>(Resource.Id.nav_list)
            };
            for (var i = 0; i < navItems.Length; i++)
            {
                var index = i;
                navItems[i].Click += (s, e) => SelectTab(index);
            }

            // TV only: the rail also reaches the screens that live outside the four tabs
            var genres = FindViewById<TextView>(Resource.Id.nav_genres);
            if (genres != null) genres.Click += (s, e) => StartActivity(new Intent(this, typeof(GenresActivity)));
            var settings = FindViewById<TextView>(Resource.Id.nav_settings);
            if (settings != null) settings.Click += (s, e) => StartActivity(new Intent(this, typeof(SettingsActivity)));

            // back from any tab returns to Home first; from Home the system takes over (predictive back friendly)
            backCallback = new ActionBackCallback(() => SelectTab(0));
            OnBackPressedDispatcher.AddCallback(this, backCallback);

            SelectTab(savedInstanceState?.GetInt(SelectedTabKey, 0) ?? 0);

            AppCenter.Start("94d2c36e-82e2-4561-91d6-85544e9cbdb8", typeof(Analytics), typeof(Crashes));
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutInt(SelectedTabKey, Math.Max(selectedTab, 0));
        }

        void SelectTab(int index)
        {
            if (index < 0 || index >= TabTags.Length) return;
            if (index == selectedTab && SupportFragmentManager.FindFragmentByTag(TabTags[index]) != null) return;

            var manager = SupportFragmentManager;
            var transaction = manager.BeginTransaction();
            transaction.SetReorderingAllowed(true);

            // show the chosen tab, hide the rest; tabs are created the first time they are opened
            for (var i = 0; i < TabTags.Length; i++)
            {
                var fragment = manager.FindFragmentByTag(TabTags[i]);
                if (i == index)
                {
                    if (fragment == null)
                    {
                        transaction.Add(Resource.Id.main_container, CreateTab(i), TabTags[i]);
                    }
                    else
                    {
                        transaction.Show(fragment);
                    }
                }
                else if (fragment != null)
                {
                    transaction.Hide(fragment);
                }
            }
            transaction.Commit();

            selectedTab = index;
            for (var i = 0; i < navItems.Length; i++)
            {
                navItems[i].Selected = i == index;
            }
            if (backCallback != null) backCallback.Enabled = index != 0;
        }

        static AndroidX.Fragment.App.Fragment CreateTab(int index)
        {
            switch (index)
            {
                case 1: return new SearchFragment();
                case 2: return new ScheduleFragment();
                case 3: return new MyListFragment();
                default: return new HomeFragment();
            }
        }
    }

    /// <summary>OnBackPressedCallback that runs a delegate (top-level so the Android wrapper is generated normally).</summary>
    public class ActionBackCallback : OnBackPressedCallback
    {
        readonly Action action;

        public ActionBackCallback(Action action) : base(false)
        {
            this.action = action;
        }

        public override void HandleOnBackPressed() => action?.Invoke();
    }
}
