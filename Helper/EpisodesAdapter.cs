using Android.Content;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.aa.tvshows.Helper
{
    public class EpisodesAdapter<T> : RecyclerView.Adapter
    {
        List<T> Items { get; set; }
        View EmptyView { get; set; }
        View LoadingView { get; set; }

        private readonly DataEnum.DataType dataType = DataEnum.DataType.None;
        private bool isFavoritesMenuShowing = false;

        public event EventHandler<int> ItemClick = delegate { };
        public event EventHandler<int> ItemLongClick = delegate { };

        public EpisodesAdapter() : base()
        {
        }

        #region DataType CTOR
        public EpisodesAdapter(DataEnum.DataType dataType) : this()
        {
            this.dataType = dataType;
            Items = new List<T>();
        }

        public EpisodesAdapter(DataEnum.DataType dataType, View emptyView) : this(dataType)
        {
            EmptyView = emptyView;
        }

        public EpisodesAdapter(DataEnum.DataType dataType, View emptyView, View loadingView) : this(dataType, emptyView)
        {
            LoadingView = loadingView;
        }

        #endregion

        #region List CTOR
        public EpisodesAdapter(List<T> items) : this()
        {
            Items = items;
        }
        public EpisodesAdapter(List<T> items, DataEnum.DataType dataType, View emptyView) : this(items)
        {
            this.dataType = dataType;
            EmptyView = emptyView;
        }

        #endregion

        public override int ItemCount
        {
            get
            {
                var count = Items == null ? 0 : Items.Count;
                if (count == 0)
                {
                    if (EmptyView != null)
                    {
                        EmptyView.Visibility = ViewStates.Visible;
                    }
                }
                else
                {
                    if (EmptyView != null)
                    {
                        EmptyView.Visibility = ViewStates.Gone;
                    }
                }
                return count;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            // bind to data
            if (holder == null)
            {
                throw new ArgumentNullException(nameof(holder));
            }

            if (holder is EpisodesViewHolder epHolder)
            {
                switch (epHolder.ItemType)
                {
                    case DataEnum.DataType.NewPopularEpisodes:
                    case DataEnum.DataType.NewEpisodes:
                        var epItem = Items[position] as EpisodeList;
                        Picasso.With(epHolder.ItemView.Context).Load(epItem.ImageLink).Into(epHolder.Image);
                        epHolder.Title.Text = epItem.Title;
                        epHolder.EpisodeDetail.Text = epItem.EpisodeNo;
                        //epHolder.Info.Text = newEpItem.ShowDetail;    // not implemented
                        break;

                    case DataEnum.DataType.PopularShows:
                        var showItem = Items[position] as ShowList;
                        Picasso.With(epHolder.ItemView.Context).Load(showItem.ImageLink).Into(epHolder.Image);
                        epHolder.Title.Text = showItem.Title;
                        epHolder.EpisodeDetail.Text = showItem.EpisodeNo;
                        epHolder.Description.Text = showItem.EpisodeDetail;
                        break;

                    case DataEnum.DataType.UserFavorites:
                        var favItem = Items[position] as SeriesDetails;
                        Picasso.With(epHolder.ItemView.Context).Load(favItem.ImageLink).Into(epHolder.Image);
                        epHolder.Title.Text = favItem.Title;
                        var episodesCount = 0;
                        favItem.Seasons.ForEach(a => episodesCount += a.Episodes.Count);
                        epHolder.EpisodeDetail.Text = string.Format("Total Seasons: {0} - Total Episodes: {1}",
                            favItem.Seasons.Count, episodesCount);
                        epHolder.LastEpisode.Text = favItem.LastEpisode is null ? "Last Episode: Unknown" :
                            string.Format("Last Episode: {0} {1}", favItem.LastEpisode?.EpisodeFullNameNumber, favItem.LastEpisode.EpisodeAirDate);
                        epHolder.NextEpisode.Text = favItem.NextEpisode is null ? "Next Episode: Unknown" :
                            string.Format("Next Episode: {0} {1}", favItem.NextEpisode?.EpisodeFullNameNumber, favItem.NextEpisode.EpisodeAirDate);
                        epHolder.Description.Text = favItem.Description;
                        epHolder.FavoritesBtn.Click += delegate { ShowFavoritesMenu(epHolder, position); };
                        break;

                    case DataEnum.DataType.TVSchedule:
                        var scheduleItem = Items[position] as CalenderScheduleList;
                        Picasso.With(epHolder.ItemView.Context).Load(scheduleItem.ImageLink).Into(epHolder.Image);
                        epHolder.Title.Text = scheduleItem.Title;
                        epHolder.EpisodeDetail.Text = scheduleItem.EpisodeNo;
                        epHolder.Description.Text = scheduleItem.EpisodeName;
                        break;

                    case DataEnum.DataType.Genres:
                        var genreItem = Items[position] as GenresShow;
                        epHolder.Title.Text = genreItem.Title;
                        epHolder.EpisodeDetail.Text = genreItem.ReleaseYear;
                        break;

                    case DataEnum.DataType.SearchSuggestions:
                        var suggestion = Items[position] as SearchSuggestionsData;
                        epHolder.Title.Text = suggestion.Title;
                        break;

                    case DataEnum.DataType.Search:
                        var searchItem = Items[position] as SearchList;
                        epHolder.EpisodeDetail.Text = searchItem.EpisodeNo;
                        Picasso.With(epHolder.ItemView.Context).Load(searchItem.ImageLink).Into(epHolder.Image);
                        epHolder.Description.Text = searchItem.EpisodeDetail;
                        epHolder.Title.Text = searchItem.Title;
                        break;

                    case DataEnum.DataType.SeasonsEpisodes:
                        var episodeItem = Items[position] as ShowEpisodeDetails;
                        epHolder.Title.Text = episodeItem.EpisodeShowTitle;
                        epHolder.EpisodeDetail.Text = episodeItem.EpisodeAirDate;
                        break;

                    case DataEnum.DataType.EpisodeStreamLinks:
                        var streamLink = Items[position] as EpisodeStreamLink;
                        epHolder.Title.Text = streamLink.HostName;
                        Picasso.With(epHolder.ItemView.Context).Load(streamLink.HostImage).Into(epHolder.Image);
                        break;

                    default: break;
                }
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // create layout
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            View itemView = null;
            if (viewType != (int)DataEnum.DataType.None)
            {
                switch (viewType)
                {
                    case (int)DataEnum.DataType.NewPopularEpisodes:
                    case (int)DataEnum.DataType.NewEpisodes:
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.episodes_list_layout, parent, false);
                        break;

                    case (int)DataEnum.DataType.PopularShows:
                    case (int)DataEnum.DataType.TVSchedule:
                    case (int)DataEnum.DataType.Search:
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.shows_list_layout, parent, false);
                        break;

                    case (int)DataEnum.DataType.Genres:
                    case (int)DataEnum.DataType.SeasonsEpisodes:
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.genres_list_show, parent, false);
                        break;

                    case (int)DataEnum.DataType.SearchSuggestions:
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_suggestions_list, parent, false);
                        break;

                    case (int)DataEnum.DataType.EpisodeStreamLinks:
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.episode_stream_list, parent, false);
                        break;

                    case (int)DataEnum.DataType.UserFavorites:
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.user_favorites_list, parent, false);
                        break;

                    default: break;
                }
            }
            var holder = new EpisodesViewHolder(itemView, (DataEnum.DataType)viewType, OnClick, OnLongClick);
            return holder;
        }

        private void ShowFavoritesMenu(EpisodesViewHolder holder, int position)
        {
            var favItem = GetItem(position) as SeriesDetails;
            var popup = new PopupMenu(holder.FavoritesBtn.Context, holder.FavoritesBtn);

            popup.MenuItemClick += async (s, e) =>
            {
                holder.FavoritesBtn.Enabled = false;
                if (e.Item.ItemId == 1)         // update
                    {
                    holder.ProgressBar.Visibility = ViewStates.Visible;
                    if (await WebData.GetDetailsForTVShowSeries(favItem.SeriesLink) is SeriesDetails updatedFavItem)
                    {
                        if (await StorageData.SaveSeriesToFavoritesFile(updatedFavItem))
                        {
                            UpdateItemAtPosition(position, (T)(object)updatedFavItem);
                        }
                    }
                    holder.ProgressBar.Visibility = ViewStates.Gone;
                }
                else if (e.Item.ItemId == 2)    // delete
                {
                    if (await StorageData.RemoveSeriesFromFavoritesFile(favItem))
                        RemoveItemAtPosition(position);
                }
                holder.FavoritesBtn.Enabled = true;
            };
            
            popup.DismissEvent += delegate
            {
                isFavoritesMenuShowing = false;
            };

            popup.Menu.Add(0, 1, 1, "Update").SetIcon(Android.Resource.Drawable.IcPopupSync).SetShowAsAction(ShowAsAction.Never);
            popup.Menu.Add(0, 2, 2, "Remove").SetIcon(Android.Resource.Drawable.IcMenuDelete).SetShowAsAction(ShowAsAction.Never);
            
            if (!isFavoritesMenuShowing)
            {
                popup.Show();
                isFavoritesMenuShowing = true;
            }
        }

        public override async void OnAttachedToRecyclerView(RecyclerView recyclerView)
        {
            base.OnAttachedToRecyclerView(recyclerView);
            if (recyclerView == null)
            {
                throw new ArgumentNullException(nameof(recyclerView));
            }
            recyclerView.HasFixedSize = true;
            recyclerView.SetItemViewCacheSize(20);
            if (dataType != DataEnum.DataType.None)
            {
                if (LoadingView != null)
                {
                    LoadingView.Visibility = ViewStates.Visible;
                }

                if (EmptyView != null)
                {
                    (EmptyView as AppCompatTextView).Text = "Loading...";
                    EmptyView.Visibility = ViewStates.Visible;
                }

                if (dataType == DataEnum.DataType.NewPopularEpisodes)
                {
                    var items = await WebData.GetPopularEpisodesForMainView().ConfigureAwait(true);
                    if (items != null && recyclerView.GetAdapter() is EpisodesAdapter<EpisodeList> adapter)
                    {
                        adapter.AddItem(items.ToArray());
                    }
                }
                else if (dataType == DataEnum.DataType.PopularShows)
                {
                    var items = await WebData.GetPopularShowsForMainView().ConfigureAwait(true);
                    if (items != null && recyclerView.GetAdapter() is EpisodesAdapter<EpisodeList> adapter)
                    {
                        adapter.AddItem(items.ToArray());
                    }
                }
                else if (dataType == DataEnum.DataType.NewEpisodes)
                {
                    var items = await WebData.GetNewestEpisodesForMainView().ConfigureAwait(true);
                    if (items != null && recyclerView.GetAdapter() is EpisodesAdapter<EpisodeList> adapter)
                    {
                        adapter.AddItem(items.ToArray());
                    }
                }
                else if (dataType == DataEnum.DataType.UserFavorites)
                {
                    var items = await StorageData.GetSeriesListFromFavoritesFile().ConfigureAwait(true);
                    if (items != null && recyclerView.GetAdapter() is EpisodesAdapter<SeriesDetails> adapter)
                    {
                        adapter.AddItem(items.ToArray());
                    }
                }

                if (LoadingView != null)
                {
                    LoadingView.Visibility = ViewStates.Invisible;
                }

                if (EmptyView != null)
                {
                    (EmptyView as AppCompatTextView).Text = EmptyView.Resources.GetString(Resource.String.empty_data_view);
                    if (ItemCount <= 0)
                    {
                        EmptyView.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        EmptyView.Visibility = ViewStates.Gone;
                    }
                }
            }
        }

        public override int GetItemViewType(int position)
        {
            var type = Items[position].GetType();
            if (type == typeof(ShowList))
            {
                var item = Items[position] as ShowList;
                return (int)item.ItemType;
            }
            if (type == typeof(EpisodeList))
            {
                var item = Items[position] as EpisodeList;
                return (int)item.ItemType;
            }
            if (type == typeof(CalenderScheduleList))
            {
                var item = Items[position] as CalenderScheduleList;
                return (int)item.ItemType;
            }
            if (type == typeof(GenresShow))
            {
                return (int)DataEnum.DataType.Genres;
            }
            if (type == typeof(SearchSuggestionsData))
            {
                return (int)DataEnum.DataType.SearchSuggestions;
            }
            if (type == typeof(SearchList))
            {
                return (int)DataEnum.DataType.Search;
            }
            if (type == typeof(ShowEpisodeDetails))
            {
                return (int)DataEnum.DataType.SeasonsEpisodes;
            }
            if (type == typeof(EpisodeStreamLink))
            {
                return (int)DataEnum.DataType.EpisodeStreamLinks;
            }
            if (type == typeof(SeriesDetails))
            {
                return (int)DataEnum.DataType.UserFavorites;
            }

            return (int)DataEnum.DataType.None;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        private void OnLongClick(int position)
        {
            ItemLongClick?.Invoke(this, position);
        }

        public void AddItem(params T[] items)
        {
            if (items != null)
            {
                var currentPos = Items.Count - 1;
                Items.AddRange(items);
                NotifyItemRangeInserted(currentPos, items.Length);
                NotifyDataSetChanged();
            }
        }

        public T GetItem(int position)
        {
            if (Items != null && Items.Count > position)
            {
                return Items[position];
            }
            return default;
        }

        public void RemoveItemAtPosition(int position)
        {
            if (Items != null)
            {
                Items.RemoveAt(position);
                NotifyItemRemoved(position);
                NotifyDataSetChanged();
            }
        }

        public void UpdateItemAtPosition(int position, T item)
        {
            if (Items != null)
            {
                RemoveItemAtPosition(position);
                Items.Insert(position, item);
                NotifyItemChanged(position);
                NotifyDataSetChanged();
            }
        }

        public void ResetAdapter()
        {
            Items.Clear();
            NotifyDataSetChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Items.Clear();
                Items = null;
                EmptyView = null;
                LoadingView = null;
            }

            base.Dispose(disposing);
        }
    }

    public class EpisodesViewHolder : RecyclerView.ViewHolder
    {
        public AppCompatImageView Image { get; private set; }
        public AppCompatTextView Title { get; private set; }
        public AppCompatTextView EpisodeDetail { get; private set; }
        public AppCompatTextView Description { get; private set; }

        public AppCompatTextView LastEpisode { get; private set; }
        public AppCompatTextView NextEpisode { get; private set; }
        public AppCompatImageButton FavoritesBtn { get; private set; }
        public ContentLoadingProgressBar ProgressBar { get; private set; }

        public DataEnum.DataType ItemType { get; private set; }

        public EpisodesViewHolder(View itemView, DataEnum.DataType type, Action<int> itemClick, Action<int> itemLongClick) : base(itemView)
        {
            if (itemView == null)
            {
                throw new ArgumentNullException(nameof(itemView));
            }

            ItemType = type;
            if (ItemType == DataEnum.DataType.NewPopularEpisodes || ItemType == DataEnum.DataType.NewEpisodes)
            {
                Image = itemView.FindViewById<AppCompatImageView>(Resource.Id.episodes_list_imageView);
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.episodes_list_title);
                EpisodeDetail = itemView.FindViewById<AppCompatTextView>(Resource.Id.episodes_list_detail);
            }
            else if (ItemType == DataEnum.DataType.PopularShows || ItemType == DataEnum.DataType.TVSchedule || ItemType == DataEnum.DataType.Search)
            {
                Image = itemView.FindViewById<AppCompatImageView>(Resource.Id.shows_list_imageView);
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.shows_list_title);
                EpisodeDetail = itemView.FindViewById<AppCompatTextView>(Resource.Id.shows_list_episode_detail);
                Description = itemView.FindViewById<AppCompatTextView>(Resource.Id.shows_list_info_detail);
            }
            else if (ItemType == DataEnum.DataType.Genres || ItemType == DataEnum.DataType.SeasonsEpisodes)
            {
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.genres_show_title);
                EpisodeDetail = itemView.FindViewById<AppCompatTextView>(Resource.Id.genres_show_year);
            }
            else if (ItemType == DataEnum.DataType.SearchSuggestions)
            {
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.search_suggestion_text);
            }
            else if (ItemType == DataEnum.DataType.EpisodeStreamLinks)
            {
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.stream_host_titletext);
                Image = itemView.FindViewById<AppCompatImageView>(Resource.Id.stream_host_imageview);
            }
            else if (ItemType == DataEnum.DataType.UserFavorites)
            {
                Image = itemView.FindViewById<AppCompatImageView>(Resource.Id.favorites_list_imageView);
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.favorites_list_title);
                EpisodeDetail = itemView.FindViewById<AppCompatTextView>(Resource.Id.favorites_list_episode_detail);
                Description = itemView.FindViewById<AppCompatTextView>(Resource.Id.favorites_list_info_detail);
                NextEpisode = itemView.FindViewById<AppCompatTextView>(Resource.Id.favorites_list_next_ep_detail);
                LastEpisode = itemView.FindViewById<AppCompatTextView>(Resource.Id.favorites_list_last_ep_detail);
                FavoritesBtn = itemView.FindViewById<AppCompatImageButton>(Resource.Id.favorites_list_remove_btn);
                ProgressBar = itemView.FindViewById<ContentLoadingProgressBar>(Resource.Id.favorites_list_loading);
            }

            itemView.Click += (s, e) => itemClick?.Invoke(AdapterPosition);
            itemView.LongClick += (s, e) => itemLongClick?.Invoke(AdapterPosition);
        }
    }

    public class CachingLayoutManager : LinearLayoutManager
    {
        public CachingLayoutManager(Context context) : base(context)
        {
            base.AutoMeasureEnabled = true;
            base.ItemPrefetchEnabled = true;
            base.MeasurementCacheEnabled = true;
        }
    }
}