using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Helper
{
    public enum CardLayout
    {
        Poster = 0,
        Wide = 1
    }

    /// <summary>
    /// Poster / backdrop tiles. One adapter serves the horizontal home rows and the search,
    /// genre and My List grids; the phone and TV differences live in the layouts and dimens.
    /// </summary>
    public class CardAdapter : RecyclerView.Adapter
    {
        readonly List<MediaCard> items;
        readonly CardLayout layout;
        readonly bool isTv;

        public event Action<MediaCard> ItemClick;
        public event Action<MediaCard, View> ItemLongClick;
        public event Action<MediaCard> ItemFocused;

        /// <summary>When above zero the tile is a grid cell of this width in px; its height follows at 2:3.</summary>
        public int GridCellWidth { get; set; }

        public CardAdapter(IEnumerable<MediaCard> source, CardLayout layout, bool isTv)
        {
            items = new List<MediaCard>(source ?? new List<MediaCard>());
            this.layout = layout;
            this.isTv = isTv;
        }

        public override int ItemCount => items.Count;

        public override int GetItemViewType(int position) => (int)layout;

        public MediaCard GetItem(int position) => position >= 0 && position < items.Count ? items[position] : null;

        public void Add(IEnumerable<MediaCard> more)
        {
            if (more == null) return;
            var start = items.Count;
            items.AddRange(more);
            if (items.Count > start) NotifyItemRangeInserted(start, items.Count - start);
        }

        public void Replace(IEnumerable<MediaCard> source)
        {
            items.Clear();
            if (source != null) items.AddRange(source);
            NotifyDataSetChanged();
        }

        public void Clear() => Replace(null);

        public void Remove(MediaCard card)
        {
            var index = items.IndexOf(card);
            if (index < 0) return;
            items.RemoveAt(index);
            NotifyItemRemoved(index);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var res = viewType == (int)CardLayout.Wide ? Resource.Layout.item_card_wide : Resource.Layout.item_card_poster;
            var view = LayoutInflater.From(parent.Context).Inflate(res, parent, false);
            return new CardHolder(view, isTv,
                position => ItemClick?.Invoke(GetItem(position)),
                (position, anchor) => ItemLongClick?.Invoke(GetItem(position), anchor),
                position => ItemFocused?.Invoke(GetItem(position)));
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is CardHolder card)
            {
                card.Bind(items[position], layout, GridCellWidth);
            }
        }
    }

    public class CardHolder : RecyclerView.ViewHolder
    {
        readonly ImageView image;
        readonly TextView title;
        readonly TextView badge;

        public CardHolder(View itemView, bool isTv, Action<int> click, Action<int, View> longClick, Action<int> focused) : base(itemView)
        {
            image = itemView.FindViewById<ImageView>(Resource.Id.card_image);
            title = itemView.FindViewById<TextView>(Resource.Id.card_title);
            badge = itemView.FindViewById<TextView>(Resource.Id.card_badge);

            itemView.Click += (s, e) =>
            {
                var position = AdapterPosition;
                if (position >= 0) click(position);
            };
            itemView.LongClick += (s, e) =>
            {
                var position = AdapterPosition;
                if (position >= 0) longClick(position, itemView);
                e.Handled = true;
            };
            itemView.FocusChange += (s, e) =>
            {
                if (isTv)
                {
                    // the focused tile grows and floats above its neighbours
                    var scale = e.HasFocus ? 1.08f : 1f;
                    itemView.Animate().ScaleX(scale).ScaleY(scale).SetDuration(120).Start();
                    itemView.TranslationZ = e.HasFocus ? 8f * itemView.Resources.DisplayMetrics.Density : 0f;
                }
                if (e.HasFocus)
                {
                    var position = AdapterPosition;
                    if (position >= 0) focused(position);
                }
            };
            if (!isTv)
            {
                // a quick tactile press-in on touch, not just a flat ripple
                itemView.Touch += (s, e) =>
                {
                    switch (e.Event.Action)
                    {
                        case MotionEventActions.Down:
                            itemView.Animate().ScaleX(0.96f).ScaleY(0.96f).SetDuration(100).Start();
                            break;
                        case MotionEventActions.Up:
                        case MotionEventActions.Cancel:
                            itemView.Animate().ScaleX(1f).ScaleY(1f).SetDuration(120).Start();
                            break;
                    }
                    e.Handled = false;
                };
            }
        }

        public void Bind(MediaCard card, CardLayout layout, int gridCellWidth)
        {
            if (gridCellWidth > 0)
            {
                var lp = ItemView.LayoutParameters as ViewGroup.MarginLayoutParams;
                if (lp != null)
                {
                    var margin = (int)(5 * ItemView.Resources.DisplayMetrics.Density);
                    lp.SetMargins(margin, margin, margin, margin);
                    lp.Width = ViewGroup.LayoutParams.MatchParent;
                    lp.Height = (int)((gridCellWidth - 2 * margin) * 1.5f);
                    ItemView.LayoutParameters = lp;
                }
            }

            var url = layout == CardLayout.Wide ? card.Backdrop("w780") ?? card.Poster("w342") : card.Poster("w342");
            AppView.LoadPicture(image, url);

            title.Text = card.Title;
            // wide tiles always carry their title; poster tiles only need it when there is no artwork
            title.Visibility = layout == CardLayout.Wide || string.IsNullOrEmpty(url) ? ViewStates.Visible : ViewStates.Gone;

            if (string.IsNullOrEmpty(card.Badge))
            {
                badge.Visibility = ViewStates.Gone;
            }
            else
            {
                badge.Text = card.Badge;
                badge.Visibility = ViewStates.Visible;
            }
        }
    }
}
