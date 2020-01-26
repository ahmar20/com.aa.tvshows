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
    public class GenresActivity : RefreshableFragment
    {
        ViewPager pager;
        TabLayout tabLayout;

        private List<string> Genres => new List<string>()
        {
            "Action", "Adventure", "Animation", "Comedy", "Crime", "Documentary", "Drama", "Family", "Fantasy", "Game-Show", "History", "Horror",
            "Human Drama", "Japanese", "Music", "Mystery", "Reality-TV", "Romance", "School", "Sci-Fi", "Sport", "Talk-Show", "Thriller", "War", "Western"
        };


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = LayoutInflater.Inflate(Resource.Layout.genres_base, container, false);
            // generate tabs for genres
            SetupTabsForGenres(view);
            return view;
        }

        public override void refresh()
        {

            if (pager.Adapter is PageTabsAdapter adapter)
            {
                var fragment = adapter.GetTabFragment(tabLayout.SelectedTabPosition);
                if (fragment is RefreshableFragment frag)
                {
                    frag?.refresh();
                }
            }


        }
        PageTabsAdapter adapter;
        private void SetupTabsForGenres(View v)
        {
            pager = v.FindViewById<ViewPager>(Resource.Id.main_tabs_viewpager);
            tabLayout = v.FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            tabLayout.SetupWithViewPager(pager);
            tabLayout.TabMode = TabLayout.ModeScrollable;

            if (adapter == null) adapter = new PageTabsAdapter(ChildFragmentManager);
            if (pager.Adapter == null) pager.Adapter = adapter;
            if (adapter.Fragments != null)
                adapter.Fragments.Clear();
            foreach (var item in Genres)
            {
                adapter.AddTab(new TitleFragment() { Title = item, Fragmnet = new MainTabs(DataEnum.DataType.Genres, DataEnum.GenreDataType.Shows, item, 0) });
            }
        }
    }
}