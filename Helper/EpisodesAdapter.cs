using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using Square.Picasso;

namespace com.aa.tvshows.Helper
{
    public class EpisodesAdapter<T> : RecyclerView.Adapter
    {
        List<T> Items { get; set; }
        View EmptyView { get; set; }
        View LoadingView { get; set; }
        int LoadMoreItemsCurrentPage { get; set; } = 1;

        private readonly DataEnum.MainTabsType dataType = DataEnum.MainTabsType.None;
        private readonly DataEnum.GenreDataType genresType;
        private readonly string genre;
        private readonly int year;
        public event EventHandler<int> ItemClick = delegate { };
        public event EventHandler<int> ItemLongClick = delegate { };

        #region DataType CTOR
        public EpisodesAdapter(DataEnum.MainTabsType dataType)
        {
            this.dataType = dataType;
            Items = new List<T>();
        }

        public EpisodesAdapter(DataEnum.MainTabsType dataType, View emptyView) : this(dataType)
        {
            this.EmptyView = emptyView;
        }

        public EpisodesAdapter(DataEnum.MainTabsType dataType, View emptyView, View loadingView) : this(dataType, emptyView)
        {
            this.LoadingView = loadingView;
        }

        #endregion

        #region Genres CTOR

        public EpisodesAdapter(DataEnum.MainTabsType dataType, DataEnum.GenreDataType genresType) : this(dataType)
        {
            this.genresType = genresType;
        }

        public EpisodesAdapter(DataEnum.MainTabsType dataType, DataEnum.GenreDataType genresType, View emptyView) : this(dataType, genresType)
        {
            this.EmptyView = emptyView;
        }

        public EpisodesAdapter(DataEnum.MainTabsType dataType, DataEnum.GenreDataType genresType, View emptyView, View loadingView) : this(dataType, genresType, emptyView)
        {
            this.LoadingView = loadingView;
        }

        public EpisodesAdapter(DataEnum.MainTabsType dataType, DataEnum.GenreDataType genresType, string genre, int year, View emptyView, View loadingView) : this(dataType, genresType, emptyView, loadingView)
        {
            this.genre = genre;
            this.year = year;
        }

        #endregion

        #region List CTOR
        public EpisodesAdapter(List<T> items)
        {
            this.Items = items;
        }
        public EpisodesAdapter(List<T> items, DataEnum.MainTabsType dataType, View emptyView) : this(items)
        {
            this.dataType = dataType;
            this.EmptyView = emptyView;
        }

        #endregion

        public override int ItemCount
        {
            get
            {
                /*
                if (Items == null || Items.Count == 0)
                {
                    if (EmptyView != null)
                        EmptyView.Visibility = ViewStates.Visible;
                }
                else
                {
                    if (EmptyView != null)
                        EmptyView.Visibility = ViewStates.Gone;
                }
                */
                return Items == null ? 0 : Items.Count;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            // bind to data
            if (holder == null) throw new ArgumentNullException(nameof(holder));

            if (holder is EpisodesViewHolder epHolder)
            {
                switch (epHolder.ItemType)
                {
                    case DataEnum.MainTabsType.NewPopularEpisodes:
                    case DataEnum.MainTabsType.NewEpisodes:
                        var epItem = Items[position] as EpisodeList;
                        Picasso.With(epHolder.ItemView.Context).Load(epItem.ImageLink).Into(epHolder.Image);
                        epHolder.Title.Text = epItem.Title;
                        epHolder.Detail.Text = epItem.EpisodeNo;
                        //epHolder.Info.Text = newEpItem.ShowDetail;    // not implemented
                        break;

                    case DataEnum.MainTabsType.PopularShows:
                        var showItem = Items[position] as ShowList;
                        Picasso.With(epHolder.ItemView.Context).Load(showItem.ImageLink).Into(epHolder.Image);
                        epHolder.Title.Text = showItem.Title;
                        epHolder.Detail.Text = showItem.EpisodeNo;
                        epHolder.Info.Text = showItem.EpisodeDetail;
                        break;

                    case DataEnum.MainTabsType.TVSchedule:
                        var scheduleItem = Items[position] as CalenderScheduleList;
                        Picasso.With(epHolder.ItemView.Context).Load(scheduleItem.ImageLink).Into(epHolder.Image);
                        epHolder.Title.Text = scheduleItem.Title;
                        epHolder.Detail.Text = scheduleItem.EpisodeNo;
                        epHolder.Info.Text = scheduleItem.EpisodeName;
                        break;

                    case DataEnum.MainTabsType.Genres:
                        var genreItem = Items[position] as GenresShow;
                        epHolder.Title.Text = genreItem.Title;
                        epHolder.Detail.Text = genreItem.ReleaseYear;
                        break;

                    default:
                        break;
                }
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // create layout
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            if (viewType == (int)DataEnum.MainTabsType.NewPopularEpisodes || viewType == (int)DataEnum.MainTabsType.NewEpisodes)
            {
                var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.episodes_list_layout, parent, false);
                var holder = new EpisodesViewHolder(itemView, (DataEnum.MainTabsType)viewType, OnClick, OnLongClick);
                return holder;
            }
            else if (viewType == (int)DataEnum.MainTabsType.PopularShows || viewType == (int)DataEnum.MainTabsType.TVSchedule)
            {
                var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.shows_list_layout, parent, false);
                var holder = new EpisodesViewHolder(itemView, (DataEnum.MainTabsType)viewType, OnClick, OnLongClick);
                return holder;
            }
            else if (viewType == (int)DataEnum.MainTabsType.Genres)
            {
                var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.genres_list_show, parent, false);
                var holder = new EpisodesViewHolder(itemView, (DataEnum.MainTabsType)viewType, OnClick, OnLongClick);
                return holder;
            }
            return null;
        }

        public override async void OnAttachedToRecyclerView(RecyclerView recyclerView)
        {
            base.OnAttachedToRecyclerView(recyclerView);
            if (recyclerView == null) throw new ArgumentNullException(nameof(recyclerView));
            
            if (dataType != DataEnum.MainTabsType.None)
            {
                if (LoadingView != null)
                    LoadingView.Visibility = ViewStates.Visible;
                if (EmptyView != null)
                {
                    (EmptyView as AppCompatTextView).Text = "Loading...";
                    EmptyView.Visibility = ViewStates.Visible;
                }
                if (dataType == DataEnum.MainTabsType.NewPopularEpisodes)
                {
                    var items = await WebData.GetPopularEpisodesForMainView().ConfigureAwait(true);
                    items?.ForEach(a =>
                    {
                        if (!(a is T _item))
                        {
                            return;
                        }
                        AddItem(_item);
                    });
                    items?.Clear();
                }
                else if (dataType == DataEnum.MainTabsType.PopularShows)
                {
                    var items = await WebData.GetPopularShowsForMainView().ConfigureAwait(true);
                    items?.ForEach(a =>
                    {
                        if (!(a is T _item))
                        {
                            return;
                        }
                        AddItem(_item);
                    });
                    items?.Clear();
                }
                else if (dataType == DataEnum.MainTabsType.NewEpisodes)
                {
                    var items = await WebData.GetNewestEpisodesForMainView().ConfigureAwait(true);
                    items?.ForEach(a =>
                    {
                        if (!(a is T _item))
                        {
                            return;
                        }
                        AddItem(_item);
                    });
                    items?.Clear();
                }
                else if (dataType == DataEnum.MainTabsType.Genres)
                {
                    // get the genre type and load data
                    if (genresType == DataEnum.GenreDataType.Shows)
                    {
                        recyclerView.AddOnScrollListener(new EndlessScroll((LinearLayoutManager)recyclerView.GetLayoutManager(), 
                            new Action(async() =>
                            {
                                if (LoadingView != null)
                                    LoadingView.Visibility = ViewStates.Visible;
                                var items = await WebData.GetGenresShows(genre, LoadMoreItemsCurrentPage++, year);
                                items?.ForEach(a => { if (!(a is T item)) return; AddItem(item); });
                                if (LoadingView != null)
                                    LoadingView.Visibility = ViewStates.Invisible;
                                (EmptyView as AppCompatTextView).Text = EmptyView.Resources.GetString(Resource.String.empty_data_view);
                                if (ItemCount <= 0)
                                    EmptyView.Visibility = ViewStates.Visible;
                                else
                                    EmptyView.Visibility = ViewStates.Gone;
                            })));
                        var items = await WebData.GetGenresShows(genre, LoadMoreItemsCurrentPage++, year);
                        items?.ForEach(a => { if (!(a is T item)) return; AddItem(item); });
                    }
                }
                if (LoadingView != null)
                    LoadingView.Visibility = ViewStates.Invisible;
                if (EmptyView != null)
                {
                    (EmptyView as AppCompatTextView).Text = EmptyView.Resources.GetString(Resource.String.empty_data_view);
                    if (ItemCount <= 0)
                        EmptyView.Visibility = ViewStates.Visible;
                    else
                        EmptyView.Visibility = ViewStates.Gone;
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
                return (int)DataEnum.MainTabsType.Genres;
            }
            return (int)DataEnum.MainTabsType.None;
        }

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        private void OnLongClick(int position)
        {
            ItemLongClick?.Invoke(this, position);
        }

        private void AddItem(params T[] items)
        {
            if (items != null)
            {
                this.Items.AddRange(items);
                NotifyDataSetChanged();
            }
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
        public ImageView Image { get; private set; }
        public AppCompatTextView Title { get; private set; }
        public AppCompatTextView Detail { get; private set; }
        public AppCompatTextView Info { get; private set; }

        public DataEnum.MainTabsType ItemType { get; private set; }

        public EpisodesViewHolder(View itemView, DataEnum.MainTabsType type, Action<int> itemClick, Action<int> itemLongClick) : base(itemView)
        {
            if (itemView == null) throw new ArgumentNullException(nameof(itemView));
            ItemType = type;
            if (ItemType == DataEnum.MainTabsType.NewPopularEpisodes || ItemType == DataEnum.MainTabsType.NewEpisodes)
            {
                Image = itemView.FindViewById<ImageView>(Resource.Id.episodes_list_imageView);
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.episodes_list_title);
                Detail = itemView.FindViewById<AppCompatTextView>(Resource.Id.episodes_list_detail);
            }
            else if (ItemType == DataEnum.MainTabsType.PopularShows || ItemType == DataEnum.MainTabsType.TVSchedule)
            {
                Image = itemView.FindViewById<ImageView>(Resource.Id.shows_list_imageView);
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.shows_list_title);
                Detail = itemView.FindViewById<AppCompatTextView>(Resource.Id.shows_list_episode_detail);
                Info = itemView.FindViewById<AppCompatTextView>(Resource.Id.shows_list_info_detail);
            }
            else if (ItemType == DataEnum.MainTabsType.Genres)
            {
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.genres_show_title);
                Detail = itemView.FindViewById<AppCompatTextView>(Resource.Id.genres_show_year);
            }

            itemView.Click += (s, e) => itemClick?.Invoke(AdapterPosition);
            itemView.LongClick += (s, e) => itemLongClick?.Invoke(AdapterPosition);
        }
    }

}