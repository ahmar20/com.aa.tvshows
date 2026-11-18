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
using AndroidX.ViewPager2.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Google.Android.Material.Tabs;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Genres", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class GenresActivity : AppCompatActivity
    {
        ViewPager2 pager;
        TabLayout tabLayout;

        protected override async Task OnCreate(Bundle savedInstanceState)
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

        // Same D-pad paging fallback as MainActivity -- see the comment there.
        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            if (pager != null)
            {
                var itemCount = pager.Adapter?.ItemCount ?? 0;
                if (keyCode == Keycode.DpadLeft && pager.CurrentItem > 0)
                {
                    pager.CurrentItem -= 1;
                    return true;
                }
                if (keyCode == Keycode.DpadRight && pager.CurrentItem < itemCount - 1)
                {
                    pager.CurrentItem += 1;
                    return true;
                }
            }
            return base.OnKeyUp(keyCode, e);
        }

        private async void SetupTabsForGenres()
        {
            pager = FindViewById<ViewPager2>(Resource.Id.main_tabs_viewpager);
            pager.OffscreenPageLimit = 3;
            tabLayout = FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            //tabLayout.SetupWithViewPager(pager);
            tabLayout.TabMode = TabLayout.ModeScrollable;

            var adapter = new PageTabsAdapter(this);
            pager.Adapter = adapter;
            TabMediatorStrategy tabStrategy = new TabMediatorStrategy(adapter.Fragments);
            TabLayoutMediator tabMediator = new TabLayoutMediator(tabLayout, pager, tabStrategy);
            tabMediator.Attach();

            // genre list now comes straight from TMDB instead of a hardcoded, site-specific list
            if (await TMDbClient.GetGenreNames() is List<string> genres && genres.Count > 0)
            {
                foreach (var item in genres)
                {
                    adapter.AddTab(new TitleFragment() { Title = item, Fragmnet = new MainTabs(DataEnum.DataType.Genres, DataEnum.GenreDataType.Shows, item, 0) });
                }
            }
            else
            {
                Error.Instance.ShowErrorTip("Could not load genres from TMDB.", this);
            }
        }
    }
}