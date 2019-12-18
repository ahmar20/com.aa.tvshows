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
using AndroidX.RecyclerView.Widget;

namespace com.aa.tvshows.Helper
{
    public class EndlessScroll : RecyclerView.OnScrollListener
    {
        public bool IsLoading { get; private set; } = false;
        private LinearLayoutManager layoutManager;
        private readonly Action loadMoreAction;
        private int firstVisibleItemPosition = 0;
        private int totalItemCount = 0;
        private int visibleItemCount = 0;
        private int previousTotal = 0;
        private readonly int visibleThreshold = 30;

        public EndlessScroll(LinearLayoutManager layoutManager, Action loadMoreAction)
        {
            this.layoutManager = layoutManager;
            this.loadMoreAction = loadMoreAction;
        }

        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
        {
            base.OnScrolled(recyclerView, dx, dy);

            if (dy > 0)
            {
                visibleItemCount = layoutManager.ChildCount;
                totalItemCount = layoutManager.ItemCount;
                firstVisibleItemPosition = layoutManager.FindFirstVisibleItemPosition();

                if (!IsLoading)
                {
                    if (totalItemCount > previousTotal)
                    {
                        IsLoading = true;
                        previousTotal = totalItemCount;
                    }
                }
                if (IsLoading && (totalItemCount - visibleItemCount)
                    <= (firstVisibleItemPosition + visibleThreshold))
                {
                    // End has been reached
                    // Do something
                    loadMoreAction?.Invoke();
                    IsLoading = false;
                }
            }
        }
    }
}