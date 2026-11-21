using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace com.aa.tvshows.Helper
{
    /// <summary>
    /// The home screen's single RecyclerView. On the phone, the hero is item 0 (so it scrolls with
    /// the list, pixel for pixel, instead of living in a separate AppBarLayout), followed by the
    /// rows. On TV the hero is a fixed view outside this RecyclerView, so there is no header item.
    /// </summary>
    public class HomeRowsAdapter : RecyclerView.Adapter
    {
        const int HeaderType = 0;
        const int RowType = 1;

        readonly List<HomeRow> rows = new List<HomeRow>();
        readonly RecyclerView.RecycledViewPool pool = new RecyclerView.RecycledViewPool();
        readonly bool isTv;
        bool HasHeader => !isTv;

        public event Action<MediaCard> CardClick;
        /// <summary>Raised with the focused tile and the position of the row it is in.</summary>
        public event Action<MediaCard, int> CardFocused;
        public event Action<HomeRow> ViewAllClick;
        public event Action HeroDetailsClick;
        public event Action HeroListClick;

        MediaCard heroCard;
        bool heroInList;

        public HomeRowsAdapter(bool isTv)
        {
            this.isTv = isTv;
        }

        public override int ItemCount => (HasHeader ? 1 : 0) + rows.Count;

        public override int GetItemViewType(int position) => HasHeader && position == 0 ? HeaderType : RowType;

        /// <summary>Binds the hero. Phone only; safe to call before any rows exist.</summary>
        public void SetHero(MediaCard card, bool inList)
        {
            if (!HasHeader) return;
            heroCard = card;
            heroInList = inList;
            NotifyItemChanged(0);
        }

        public void SetHeroListState(bool inList)
        {
            if (!HasHeader) return;
            heroInList = inList;
            NotifyItemChanged(0);
        }

        public void AddRow(HomeRow row)
        {
            rows.Add(row);
            NotifyItemInserted((HasHeader ? 1 : 0) + rows.Count - 1);
        }

        public void Clear()
        {
            heroCard = null;
            var hadRows = rows.Count > 0;
            rows.Clear();
            if (hadRows) NotifyDataSetChanged();
            else if (HasHeader) NotifyItemChanged(0);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var inflater = LayoutInflater.From(parent.Context);
            if (viewType == HeaderType)
            {
                var view = inflater.Inflate(Resource.Layout.item_home_hero, parent, false);
                return new HeroHolder(view,
                    () => HeroDetailsClick?.Invoke(),
                    () => HeroListClick?.Invoke());
            }
            var rowView = inflater.Inflate(Resource.Layout.item_row, parent, false);
            return new RowHolder(rowView, pool, isTv,
                row => ViewAllClick?.Invoke(row),
                card => CardClick?.Invoke(card),
                (card, rowHolder) => CardFocused?.Invoke(card, rowHolder.AdapterPosition));
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is HeroHolder hero)
            {
                hero.Bind(heroCard, heroInList);
                return;
            }
            if (!(holder is RowHolder rowHolder)) return;

            var row = rows[HasHeader ? position - 1 : position];
            rowHolder.Title.Text = row.Title;
            rowHolder.Row = row;
            rowHolder.Adapter.SetItems(row.Items, row.Style == CardStyle.Wide ? CardLayout.Wide : CardLayout.Poster);
        }
    }

    public class HeroHolder : RecyclerView.ViewHolder
    {
        readonly ImageView image;
        readonly TextView title, rating, meta, overview, detailsBtn, listBtn;

        public HeroHolder(View itemView, Action onDetails, Action onList) : base(itemView)
        {
            image = itemView.FindViewById<ImageView>(Resource.Id.hero_image);
            title = itemView.FindViewById<TextView>(Resource.Id.hero_title);
            rating = itemView.FindViewById<TextView>(Resource.Id.hero_rating);
            meta = itemView.FindViewById<TextView>(Resource.Id.hero_meta);
            overview = itemView.FindViewById<TextView>(Resource.Id.hero_overview);
            detailsBtn = itemView.FindViewById<TextView>(Resource.Id.hero_btn_details);
            listBtn = itemView.FindViewById<TextView>(Resource.Id.hero_btn_list);

            detailsBtn.Click += (s, e) => onDetails();
            listBtn.Click += (s, e) => onList();
        }

        public void Bind(MediaCard card, bool inList)
        {
            if (card == null)
            {
                image.SetImageDrawable(null);
                title.Text = string.Empty;
                rating.Visibility = ViewStates.Gone;
                meta.Text = string.Empty;
                overview.Text = string.Empty;
                detailsBtn.Visibility = ViewStates.Invisible;
                listBtn.Visibility = ViewStates.Invisible;
                return;
            }

            var url = card.Backdrop("w1280");
            AppView.LoadPicture(image, string.IsNullOrEmpty(url) ? card.Poster("w780") : url);
            title.Text = card.Title;
            detailsBtn.Visibility = ViewStates.Visible;
            listBtn.Visibility = ViewStates.Visible;

            if (string.IsNullOrEmpty(card.Rating))
            {
                rating.Visibility = ViewStates.Gone;
            }
            else
            {
                rating.Text = card.Rating;
                rating.Visibility = ViewStates.Visible;
            }

            var metaParts = new List<string>();
            if (!string.IsNullOrEmpty(card.Year)) metaParts.Add(card.Year);
            if (!string.IsNullOrEmpty(card.Genres)) metaParts.Add(card.Genres);
            var metaPrefix = rating.Visibility == ViewStates.Visible ? "  •  " : string.Empty;
            meta.Text = metaPrefix + string.Join("  •  ", metaParts);
            meta.Visibility = metaParts.Count == 0 ? ViewStates.Gone : ViewStates.Visible;

            overview.Text = card.Overview;
            overview.Visibility = string.IsNullOrWhiteSpace(card.Overview) ? ViewStates.Gone : ViewStates.Visible;

            listBtn.Text = listBtn.Resources.GetString(inList ? Resource.String.action_in_list : Resource.String.action_add_list);
            listBtn.SetCompoundDrawablesRelativeWithIntrinsicBounds(inList ? Resource.Drawable.ic_check : Resource.Drawable.ic_add, 0, 0, 0);
        }
    }

    public class RowHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; }
        public RecyclerView List { get; }
        public CardAdapter Adapter { get; }
        public HomeRow Row { get; set; }

        public RowHolder(View itemView, RecyclerView.RecycledViewPool pool, bool isTv,
            Action<HomeRow> viewAllClick, Action<MediaCard> onCardClick, Action<MediaCard, RowHolder> onCardFocused) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.row_title);
            List = itemView.FindViewById<RecyclerView>(Resource.Id.row_list);
            var layoutManager = new LinearLayoutManager(itemView.Context, LinearLayoutManager.Horizontal, false);
            layoutManager.InitialPrefetchItemCount = 4;
            List.SetLayoutManager(layoutManager);
            // every row shares one pool of tiles, so scrolling vertically re-uses views instead of inflating
            List.SetRecycledViewPool(pool);

            Adapter = new CardAdapter(new List<MediaCard>(), CardLayout.Poster, isTv);
            Adapter.ItemClick += card => onCardClick(card);
            Adapter.ItemFocused += card => onCardFocused(card, this);
            List.SetAdapter(Adapter);

            var viewAll = itemView.FindViewById<View>(Resource.Id.row_view_all);
            if (viewAll != null)
            {
                viewAll.Click += (s, e) => { if (Row != null) viewAllClick(Row); };
            }
        }
    }
}
