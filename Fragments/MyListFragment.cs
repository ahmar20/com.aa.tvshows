using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Fragments
{
    /// <summary>The saved shows. Press and hold a tile to update or remove it.</summary>
    public class MyListFragment : Fragment
    {
        RecyclerView list;
        ProgressBar progress;
        LinearLayoutCompat empty;
        PosterGrid grid;
        int version;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_list, container, false);
            WindowInsetsHelper.Apply(view.FindViewById<View>(Resource.Id.list_root), top: true);

            view.FindViewById<TextView>(Resource.Id.list_title).Text = GetString(Resource.String.mylist_title);
            view.FindViewById<TextView>(Resource.Id.list_subtitle).Text = GetString(Resource.String.mylist_subtitle);
            list = view.FindViewById<RecyclerView>(Resource.Id.list_rv);
            progress = view.FindViewById<ProgressBar>(Resource.Id.list_progress);
            empty = view.FindViewById<LinearLayoutCompat>(Resource.Id.list_empty);

            grid = new PosterGrid(list, Context);
            grid.Adapter.ItemClick += card => AppView.HandleItemShowEpisodeClick(card, Context);
            grid.Adapter.ItemLongClick += ShowOptions;

            _ = LoadAsync();
            return view;
        }

        // The list can change on a show's page, so refresh whenever this tab comes back into view.
        public override void OnHiddenChanged(bool hidden)
        {
            base.OnHiddenChanged(hidden);
            if (!hidden) _ = LoadAsync();
        }

        public override void OnResume()
        {
            base.OnResume();
            if (!IsHidden && grid != null) _ = LoadAsync();
        }

        public override void OnDestroyView()
        {
            version++;
            base.OnDestroyView();
        }

        async Task LoadAsync()
        {
            var current = ++version;
            var saved = await StorageData.GetSeriesListFromFavoritesFile();
            if (current != version || View == null) return;

            progress.Visibility = ViewStates.Gone;
            var cards = (saved ?? new List<SeriesDetails>()).Select(ToCard).ToList();
            grid.Adapter.Replace(cards);
            AppView.SetEmptyView(empty, cards.Count == 0, isFavorite: true);
        }

        static MediaCard ToCard(SeriesDetails series)
        {
            TMDbClient.TryParseShowLink(series.SeriesLink, out int id);
            return new MediaCard
            {
                Id = id,
                Title = series.Title,
                PageLink = series.SeriesLink,
                PosterUrlOverride = series.ImageLink,
                BackdropUrlOverride = series.BackdropLink,
                Source = series
            };
        }

        void ShowOptions(MediaCard card, View anchor)
        {
            if (!(card?.Source is SeriesDetails series)) return;

            var menu = new AndroidX.AppCompat.Widget.PopupMenu(Context, anchor);
            menu.Menu.Add(0, 1, 0, "Update");
            menu.Menu.Add(0, 2, 1, "Remove from My List");
            menu.MenuItemClick += async (s, e) =>
            {
                if (e.Item.ItemId == 1)
                {
                    var fresh = await TMDbClient.GetDetailsForTVShowSeries(series.SeriesLink, includeEpisodes: false);
                    if (fresh != null && await StorageData.SaveSeriesToFavoritesFile(fresh))
                    {
                        Error.Instance.ShowErrorTip($"{fresh.Title} updated.", Context);
                        if (View != null) await LoadAsync();
                    }
                    else
                    {
                        Error.Instance.ShowErrorTip("Could not update " + series.Title + ".", Context);
                    }
                }
                else if (await StorageData.RemoveSeriesFromFavoritesFile(series) && View != null)
                {
                    grid.Adapter.Remove(card);
                    if (grid.Adapter.ItemCount == 0) AppView.SetEmptyView(empty, true, isFavorite: true);
                }
            };
            menu.Show();
        }
    }
}
