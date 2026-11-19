using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Helper
{
    /// <summary>The home screen: a vertical list of titled, horizontally scrolling rows of tiles.</summary>
    public class HomeRowsAdapter : RecyclerView.Adapter
    {
        readonly List<HomeRow> rows = new List<HomeRow>();
        readonly RecyclerView.RecycledViewPool pool = new RecyclerView.RecycledViewPool();
        readonly bool isTv;

        public event Action<MediaCard> CardClick;
        /// <summary>Raised with the focused tile and the position of the row it is in.</summary>
        public event Action<MediaCard, int> CardFocused;

        public HomeRowsAdapter(bool isTv)
        {
            this.isTv = isTv;
        }

        public override int ItemCount => rows.Count;

        public void AddRow(HomeRow row)
        {
            rows.Add(row);
            NotifyItemInserted(rows.Count - 1);
        }

        public void Clear()
        {
            rows.Clear();
            NotifyDataSetChanged();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_row, parent, false);
            return new RowHolder(view, pool);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (!(holder is RowHolder rowHolder)) return;

            var row = rows[position];
            rowHolder.Title.Text = row.Title;

            var adapter = new CardAdapter(row.Items, row.Style == CardStyle.Wide ? CardLayout.Wide : CardLayout.Poster, isTv);
            adapter.ItemClick += card => CardClick?.Invoke(card);
            adapter.ItemFocused += card => CardFocused?.Invoke(card, rowHolder.AdapterPosition);
            rowHolder.List.SetAdapter(adapter);
        }
    }

    public class RowHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; }
        public RecyclerView List { get; }

        public RowHolder(View itemView, RecyclerView.RecycledViewPool pool) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.row_title);
            List = itemView.FindViewById<RecyclerView>(Resource.Id.row_list);
            var layoutManager = new LinearLayoutManager(itemView.Context, LinearLayoutManager.Horizontal, false);
            layoutManager.InitialPrefetchItemCount = 4;
            List.SetLayoutManager(layoutManager);
            // every row shares one pool of tiles, so scrolling vertically re-uses views instead of inflating
            List.SetRecycledViewPool(pool);
        }
    }
}
