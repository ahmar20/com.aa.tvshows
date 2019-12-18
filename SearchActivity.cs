
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
using com.aa.tvshows.Helper;

namespace com.aa.tvshows
{
    [Activity(Label = "Search", ConfigurationChanges = Android.Content.PM.ConfigChanges.ScreenLayout | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class SearchActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.search_activity);

            AppView.SetActionBarForActivity(FindViewById<Toolbar>(Resource.Id.main_toolbar), this);

            SetupSearch();
        }

        private void SetupSearch()
        {
            var editText = FindViewById<SearchView>(Resource.Id.search_text_query);
            var list = FindViewById<Android.Widget.ListView>(Resource.Id.search_list);
            list.ItemClick += (s, e) =>
            {
                // handle item click here
            };
            editText.QueryTextChange += async(s, e) =>
            {
                // get data from suggestions
                // show in listview as simple items
                e.Handled = true;
                if (e.NewText.Trim() is string query && query.Length >= 2)
                {
                    var suggestions = await WebData.GetTVShowSearchSuggestions(query);
                    list.Adapter = new SimpleListAdapter<SearchSuggestionsData>(this, suggestions);
                }
                if (e.NewText.Trim() == string.Empty)
                {
                    list.Adapter = null;
                }
            };

            editText.QueryTextSubmit += (s, e) =>
            {
                // get data from page
                // load full image based search results

            };
        }
    }
}