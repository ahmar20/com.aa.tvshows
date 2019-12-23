using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using Square.Picasso;
using System;
using System.Collections.Generic;

namespace com.aa.tvshows.Helper
{
    public class EpisodesAdapter<T> : RecyclerView.Adapter
    {
        List<T> Items { get; set; }
        View EmptyView { get; set; }
        View LoadingView { get; set; }
        int LoadMoreItemsCurrentPage { get; set; } = 1;

        private readonly DataEnum.DataType dataType = DataEnum.DataType.None;
        private readonly DataEnum.GenreDataType genresType;
        private readonly string genre;
        private readonly int year;

        public event EventHandler<int> ItemClick = delegate { };
        public event EventHandler<int> ItemLongClick = delegate { };

        #region DataType CTOR
        public EpisodesAdapter(DataEnum.DataType dataType)
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

        #region Genres CTOR

        public EpisodesAdapter(DataEnum.DataType dataType, DataEnum.GenreDataType genresType) : this(dataType)
        {
            this.genresType = genresType;
        }

        public EpisodesAdapter(DataEnum.DataType dataType, DataEnum.GenreDataType genresType, View emptyView) : this(dataType, genresType)
        {
            EmptyView = emptyView;
        }

        public EpisodesAdapter(DataEnum.DataType dataType, DataEnum.GenreDataType genresType, View emptyView, View loadingView) : this(dataType, genresType, emptyView)
        {
            LoadingView = loadingView;
        }

        public EpisodesAdapter(DataEnum.DataType dataType, DataEnum.GenreDataType genresType, string genre, int year, View emptyView, View loadingView) : this(dataType, genresType, emptyView, loadingView)
        {
            this.genre = genre;
            this.year = year;
        }

        #endregion

        #region List CTOR
        public EpisodesAdapter(List<T> items)
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
                return Items == null ? 0 : Items.Count;
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
                        var episodeItem = Items[position] as ShowEpisode;
                        epHolder.Title.Text = episodeItem.EpisodeTitle;
                        epHolder.EpisodeDetail.Text = episodeItem.EpisodeAirDate;
                        break;

                    default:break;
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

                    default: break;
                }
            }
            /*
            else
            {
                if (typeof(T) == typeof(ShowList))
                {
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.shows_list_layout, parent, false);
                }
                else if (typeof(T) == typeof(EpisodeList))
                {
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.episodes_list_layout, parent, false);
                }
            }
            */
            var holder = new EpisodesViewHolder(itemView, (DataEnum.DataType)viewType, OnClick, OnLongClick);
            return holder;
        }

        public override async void OnAttachedToRecyclerView(RecyclerView recyclerView)
        {
            base.OnAttachedToRecyclerView(recyclerView);
            if (recyclerView == null)
            {
                throw new ArgumentNullException(nameof(recyclerView));
            }

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
                else if (dataType == DataEnum.DataType.PopularShows)
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
                else if (dataType == DataEnum.DataType.NewEpisodes)
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
                else if (dataType == DataEnum.DataType.Genres)
                {
                    // get the genre type and load data
                    if (genresType == DataEnum.GenreDataType.Shows)
                    {
                        recyclerView.AddOnScrollListener(new EndlessScroll((LinearLayoutManager)recyclerView.GetLayoutManager(),
                            new Action(async () =>
                            {
                                if (LoadingView != null)
                                {
                                    LoadingView.Visibility = ViewStates.Visible;
                                }

                                var items = await WebData.GetGenresShows(genre, LoadMoreItemsCurrentPage++, year);
                                items?.ForEach(a => { if (!(a is T item)) { return; } AddItem(item); });
                                if (LoadingView != null)
                                {
                                    LoadingView.Visibility = ViewStates.Invisible;
                                } (EmptyView as AppCompatTextView).Text = EmptyView.Resources.GetString(Resource.String.empty_data_view);
                                if (ItemCount <= 0)
                                {
                                    EmptyView.Visibility = ViewStates.Visible;
                                }
                                else
                                {
                                    EmptyView.Visibility = ViewStates.Gone;
                                }
                            })));
                        var items = await WebData.GetGenresShows(genre, LoadMoreItemsCurrentPage++, year);
                        items?.ForEach(a => { if (!(a is T item)) { return; } AddItem(item); });
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
            if (type == typeof(ShowEpisode))
            {
                return (int)DataEnum.DataType.SeasonsEpisodes;
            }

            return (int)DataEnum.DataType.None;
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
                Items.AddRange(items);
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
        public AppCompatTextView EpisodeDetail { get; private set; }
        public AppCompatTextView Description { get; private set; }

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
                Image = itemView.FindViewById<ImageView>(Resource.Id.episodes_list_imageView);
                Title = itemView.FindViewById<AppCompatTextView>(Resource.Id.episodes_list_title);
                EpisodeDetail = itemView.FindViewById<AppCompatTextView>(Resource.Id.episodes_list_detail);
            }
            else if (ItemType == DataEnum.DataType.PopularShows || ItemType == DataEnum.DataType.TVSchedule || ItemType == DataEnum.DataType.Search)
            {
                Image = itemView.FindViewById<ImageView>(Resource.Id.shows_list_imageView);
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

            itemView.Click += (s, e) => itemClick?.Invoke(AdapterPosition);
            itemView.LongClick += (s, e) => itemLongClick?.Invoke(AdapterPosition);
        }
    }

}