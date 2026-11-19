using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using com.aa.tvshows.Models;

namespace com.aa.tvshows
{
    /// <summary>Pick a genre (chips on a phone, a list on the left on TV) and browse its shows as a poster grid.</summary>
    [Activity(Label = "Genres", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class GenresActivity : AppCompatActivity
    {
        RecyclerView genresList;
        RecyclerView gridView;
        ProgressBar progress;
        TextView emptyText;
        PosterGrid grid;
        EndlessScroll endless;
        GenreAdapter genreAdapter;

        List<TmdbGenre> genres = new List<TmdbGenre>();
        int genreId;
        int nextPage = 2;
        bool hasMore;
        bool loadingMore;
        int version;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            WindowInsetsHelper.EnableEdgeToEdge(this);
            SetContentView(Resource.Layout.activity_genres);
            WindowInsetsHelper.Apply(FindViewById<View>(Resource.Id.screen_root), top: true, bottom: true, sides: true);
            AppView.SetActionBarForActivity(FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.main_toolbar), this);

            genresList = FindViewById<RecyclerView>(Resource.Id.genres_list);
            gridView = FindViewById<RecyclerView>(Resource.Id.genres_grid);
            progress = FindViewById<ProgressBar>(Resource.Id.genres_progress);
            emptyText = FindViewById<TextView>(Resource.Id.genres_empty);

            var isTv = UiMode.IsTv(this);
            genresList.SetLayoutManager(new LinearLayoutManager(this, isTv ? LinearLayoutManager.Vertical : LinearLayoutManager.Horizontal, false));

            grid = new PosterGrid(gridView, this);
            grid.Adapter.ItemClick += card => AppView.HandleItemShowEpisodeClick(card, this);
            endless = new EndlessScroll(grid.Layout);
            endless.LoadMoreTask += (s, e) => _ = LoadMoreAsync();
            gridView.AddOnScrollListener(endless);

            _ = LoadGenresAsync();
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressedDispatcher.OnBackPressed();
            return true;
        }

        async Task LoadGenresAsync()
        {
            progress.Visibility = ViewStates.Visible;
            emptyText.Visibility = ViewStates.Gone;
            try
            {
                genres = await TMDbClient.GetGenres() ?? new List<TmdbGenre>();
            }
            catch (Exception)
            {
                genres = new List<TmdbGenre>();
            }
            if (IsDestroyed) return;

            if (genres.Count == 0)
            {
                progress.Visibility = ViewStates.Gone;
                emptyText.Text = GetString(Resource.String.home_load_error);
                emptyText.Visibility = ViewStates.Visible;
                return;
            }

            genreAdapter = new GenreAdapter(genres);
            genreAdapter.ItemClick += position => _ = SelectGenreAsync(position);
            genresList.SetAdapter(genreAdapter);
            await SelectGenreAsync(0);
        }

        async Task SelectGenreAsync(int position)
        {
            if (position < 0 || position >= genres.Count) return;

            genreId = genres[position].Id;
            genreAdapter.SetSelected(position);
            var current = ++version;
            grid.Adapter.Clear();
            endless.ResetState();
            hasMore = false;
            nextPage = 2;
            emptyText.Visibility = ViewStates.Gone;
            progress.Visibility = ViewStates.Visible;

            CardPage page = null;
            try
            {
                page = await TMDbClient.DiscoverByGenre(genreId, 1);
            }
            catch (Exception)
            {
            }
            if (current != version || IsDestroyed) return;

            progress.Visibility = ViewStates.Gone;
            if (page == null || page.Items.Count == 0)
            {
                emptyText.Text = GetString(Resource.String.home_load_error);
                emptyText.Visibility = ViewStates.Visible;
                return;
            }
            grid.Adapter.Replace(page.Items);
            hasMore = page.HasMore;
            gridView.ScrollToPosition(0);
        }

        async Task LoadMoreAsync()
        {
            if (!hasMore || loadingMore) return;

            loadingMore = true;
            var current = version;
            try
            {
                var page = await TMDbClient.DiscoverByGenre(genreId, nextPage);
                if (current != version || IsDestroyed) return;
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
    }
}
