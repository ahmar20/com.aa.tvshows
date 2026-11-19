using System;
using System.Collections.Generic;
using Android.Content;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Helper
{
    /// <summary>
    /// A RecyclerView set up as a poster grid whose column count follows the screen width
    /// (3 on a phone, 6+ on a TV or tablet), shared by Search, Genres and My List.
    /// </summary>
    public class PosterGrid
    {
        readonly RecyclerView view;
        readonly int reservedPx;

        public GridLayoutManager Layout { get; }
        public CardAdapter Adapter { get; }

        /// <param name="reservedPx">Width taken by anything beside the grid (e.g. the TV nav rail).</param>
        public PosterGrid(RecyclerView view, Context context, int reservedPx = 0)
        {
            this.view = view;
            this.reservedPx = reservedPx;
            Layout = new GridLayoutManager(context, 3);
            Adapter = new CardAdapter(new List<MediaCard>(), CardLayout.Poster, UiMode.IsTv(context));
            view.SetLayoutManager(Layout);
            view.SetAdapter(Adapter);
            Update(context);

            // the real width is only known after layout (and changes on rotation), so follow it
            var lastWidth = -1;
            view.LayoutChange += (s, e) =>
            {
                if (view.Width > 0 && view.Width != lastWidth)
                {
                    lastWidth = view.Width;
                    Update(context);
                }
            };
        }

        /// <summary>Recomputes columns and tile size; call again after a rotation.</summary>
        public void Update(Context context)
        {
            var metrics = context.Resources.DisplayMetrics;
            var targetPx = context.Resources.GetInteger(Resource.Integer.grid_target_dp) * metrics.Density;
            var width = view.Width > 0 ? view.Width : metrics.WidthPixels - reservedPx;
            var availablePx = width - view.PaddingLeft - view.PaddingRight;
            var span = Math.Max(3, (int)(availablePx / targetPx));
            Layout.SpanCount = span;
            Adapter.GridCellWidth = (int)(availablePx / span);
            Adapter.NotifyDataSetChanged();
        }
    }
}
