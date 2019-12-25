
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
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

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return AppView.OnOptionsItemSelected(item, this);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return AppView.ShowOptionsMenu(menu, this);
        }

        private void SetupSearch()
        {
            var editText = FindViewById<SearchView>(Resource.Id.search_text_query);
            var emptyView = FindViewById<AppCompatTextView>(Resource.Id.search_empty_text);
            var loadingView = FindViewById<ContentLoadingProgressBar>(Resource.Id.search_loading_view);
            
            var list = FindViewById<RecyclerView>(Resource.Id.search_list);
            list.ClearOnScrollListeners();
            var layoutManager = new CachingLayoutManager(this);
            list.SetLayoutManager(layoutManager);
            editText.QueryTextChange += async (s, e) =>
            {
                // get data from suggestions
                // show in listview as simple items
                e.Handled = true;
                if (e.NewText.Trim() is string query && query.Length > 2)
                {
                    emptyView.Visibility = ViewStates.Gone;
                    loadingView.Visibility = ViewStates.Visible;
                    var suggestions = await WebData.GetTVShowSearchSuggestions(query);
                    loadingView.Visibility = ViewStates.Gone;
                    var adapter = new EpisodesAdapter<SearchSuggestionsData>(suggestions, DataEnum.DataType.SearchSuggestions, null);
                    adapter.ItemClick += (s, e) =>
                    {
                        // handle click here
                        AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), this);
                    };
                    list.SetAdapter(adapter);
                }
                else
                {
                    emptyView.Visibility = ViewStates.Visible;
                    emptyView.Text = "Search query should be at least 3 characters long";
                    list.SetAdapter(null);
                }
            };

            editText.QueryTextSubmit += async(s, e) =>
            {
                // get data from page
                // load full image based search results
                e.Handled = true;
                if (e.NewText.Trim() is string query && query.Length > 2)
                {
                    list.ClearOnScrollListeners();
                    var scroll = new EndlessScroll(layoutManager);

                    loadingView.Visibility = ViewStates.Visible;
                    emptyView.Visibility = ViewStates.Gone;
                    var searchResults = await WebData.GetTVShowSearchResults(query);
                    loadingView.Visibility = ViewStates.Gone;

                    var adapter = new EpisodesAdapter<SearchList>(searchResults, DataEnum.DataType.Search, emptyView);
                    adapter.ItemClick += (s, e) =>
                    {
                        // handle item click
                        AppView.HandleItemShowEpisodeClick(adapter.GetItem(e), this);
                    };
                    list.SetAdapter(adapter);

                    scroll.LoadMoreTask += async delegate
                    {
                        if (searchResults != null && searchResults.LastOrDefault() is SearchList item)
                        {
                            if (item.NextPage > 0)
                            {
                                loadingView.Visibility = ViewStates.Visible;
                                var newItems = await WebData.GetTVShowSearchResults(query, item.NextPage).ConfigureAwait(true);
                                if (newItems != null) adapter.AddItem(newItems.ToArray());
                                loadingView.Visibility = ViewStates.Gone;
                            }
                        }
                    };

                    list.AddOnScrollListener(scroll);
                }
                else
                {
                    emptyView.Visibility = ViewStates.Visible;
                    emptyView.Text = "Search query should be at least 3 characters long";
                    list.SetAdapter(null);
                }
            };

        }
    }
}