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
using AndroidX.ViewPager.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Google.Android.Material.Tabs;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Genres", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class GenresActivity : AppCompatActivity
    {
        ViewPager pager;
        TabLayout tabLayout;

        private List<string> Genres => new List<string>() 
        {
            "Action", "Adventure", "Animation", "Comedy", "Crime", "Documentary", "Drama", "Family", "Fantasy", "Game-Show", "History", "Horror",
            "Human Drama", "Japanese", "Music", "Mystery", "Reality-TV", "Romance", "School", "Sci-Fi", "Sport", "Talk-Show", "Thriller", "War", "Western"
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.genres_base);

            AppView.SetActionBarForActivity(FindViewById<Toolbar>(Resource.Id.main_toolbar), this);

            // generate tabs for genres
            SetupTabsForGenres();
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

        private void SetupTabsForGenres()
        {
            pager = FindViewById<ViewPager>(Resource.Id.main_tabs_viewpager);
            pager.OffscreenPageLimit = 3;
            tabLayout = FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            tabLayout.SetupWithViewPager(pager);
            tabLayout.TabMode = TabLayout.ModeScrollable;

            var adapter = new PageTabsAdapter(SupportFragmentManager);
            pager.Adapter = adapter;
            foreach(var item in Genres)
            {
                adapter.AddTab(new TitleFragment() { Title = item, Fragmnet = new MainTabs(DataEnum.DataType.Genres, DataEnum.GenreDataType.Shows, item, 0) });
            }
        }
    }
}