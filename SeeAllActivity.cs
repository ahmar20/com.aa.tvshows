using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using com.aa.tvshows.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.aa.tvshows
{
    /// <summary>"View All" for a home row: the same endpoint/query, paginated, as a poster grid.</summary>
    [Activity(Label = "View All", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class SeeAllActivity : AppCompatActivity
    {
        RecyclerView gridView;
        ProgressBar progress;
        TextView emptyText;
        PosterGrid grid;
        EndlessScroll endless;

        string endpoint;
        Dictionary<string, string> query;
        int nextPage = 2;
        bool hasMore;
        bool loadingMore;

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.anim_pop_enter, Resource.Animation.anim_pop_exit);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            WindowInsetsHelper.EnableEdgeToEdge(this);
            SetContentView(Resource.Layout.activity_see_all);
            WindowInsetsHelper.Apply(FindViewById<View>(Resource.Id.screen_root), top: true, bottom: true, sides: true);

            var title = Intent.GetStringExtra("title");
            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.main_toolbar);
            AppView.SetActionBarForActivity(toolbar, this);
            SupportActionBar.Title = title;

            endpoint = Intent.GetStringExtra("endpoint");
            var queryJson = Intent.GetStringExtra("query");
            query = string.IsNullOrEmpty(queryJson) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(queryJson);

            gridView = FindViewById<RecyclerView>(Resource.Id.see_all_grid);
            progress = FindViewById<ProgressBar>(Resource.Id.see_all_progress);
            emptyText = FindViewById<TextView>(Resource.Id.see_all_empty);

            grid = new PosterGrid(gridView, this);
            grid.Adapter.ItemClick += card => AppView.HandleItemShowEpisodeClick(card, this);
            endless = new EndlessScroll(grid.Layout);
            endless.LoadMoreTask += (s, e) => _ = LoadMoreAsync();
            gridView.AddOnScrollListener(endless);

            if (string.IsNullOrEmpty(endpoint))
            {
                ShowEmpty();
            }
            else
            {
                _ = LoadFirstPageAsync();
            }
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return true;
        }

        async Task LoadFirstPageAsync()
        {
            progress.Visibility = ViewStates.Visible;
            gridView.Visibility = ViewStates.Invisible;

            CardPage page = null;
            try
            {
                page = await TMDbClient.GetCardPage(endpoint, 1, query);
            }
            catch (Exception)
            {
            }
            if (IsDestroyed) return;

            progress.Visibility = ViewStates.Gone;
            gridView.Visibility = ViewStates.Visible;
            if (page == null || page.Items.Count == 0)
            {
                ShowEmpty();
                return;
            }
            grid.Adapter.Replace(page.Items);
            hasMore = page.HasMore;
            nextPage = 2;
        }

        async Task LoadMoreAsync()
        {
            if (!hasMore || loadingMore) return;

            loadingMore = true;
            try
            {
                var page = await TMDbClient.GetCardPage(endpoint, nextPage, query);
                if (IsDestroyed) return;
                if (page != null)
                {
                    grid.Adapter.Add(page.Items);
                    hasMore = page.HasMore;
                    nextPage++;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                loadingMore = false;
            }
        }

        void ShowEmpty()
        {
            gridView.Visibility = ViewStates.Invisible;
            emptyText.Text = GetString(Resource.String.home_load_error);
            emptyText.Visibility = ViewStates.Visible;
        }
    }
}
