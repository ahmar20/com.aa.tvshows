using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace com.aa.tvshows.Helper
{
    public abstract class EndlessRecyclerViewScrollListener : RecyclerView.OnScrollListener
    {
        private readonly int visibleThreshold = 10;
        // The current offset index of data you have loaded
        private int currentPage = 0;
        // The total number of items in the dataset after the last load
        private int previousTotalItemCount = 0;
        // True if we are still waiting for the last set of data to load.
        private bool loading = true;
        // Sets the starting page index
        private readonly int startingPageIndex = 0;

        readonly RecyclerView.LayoutManager mLayoutManager;

        public EndlessRecyclerViewScrollListener(LinearLayoutManager layoutManager)
        {
            this.mLayoutManager = layoutManager;
        }

        public EndlessRecyclerViewScrollListener(GridLayoutManager layoutManager)
        {
            this.mLayoutManager = layoutManager;
            visibleThreshold *= layoutManager.SpanCount;
        }

        public EndlessRecyclerViewScrollListener(StaggeredGridLayoutManager layoutManager)
        {
            this.mLayoutManager = layoutManager;
            visibleThreshold *= layoutManager.SpanCount;
        }

        public int GetLastVisibleItem(int[] lastVisibleItemPositions)
        {
            int maxSize = 0;
            for (int i = 0; i < lastVisibleItemPositions.Length; i++)
            {
                if (i == 0)
                {
                    maxSize = lastVisibleItemPositions[i];
                }
                else if (lastVisibleItemPositions[i] > maxSize)
                {
                    maxSize = lastVisibleItemPositions[i];
                }
            }
            return maxSize;
        }

        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
        {
            int lastVisibleItemPosition = 0;
            int totalItemCount = mLayoutManager.ItemCount;

            if (mLayoutManager is StaggeredGridLayoutManager staggeredLayout)
            {
                int[] lastVisibleItemPositions = staggeredLayout.FindLastVisibleItemPositions(null);
                // get maximum element within the list
                lastVisibleItemPosition = GetLastVisibleItem(lastVisibleItemPositions);
            }
            else if (mLayoutManager is GridLayoutManager gridLayout)
            {
                lastVisibleItemPosition = gridLayout.FindLastVisibleItemPosition();
            }
            else if (mLayoutManager is LinearLayoutManager linearLayout)
            {
                lastVisibleItemPosition = linearLayout.FindLastVisibleItemPosition();
            }

            // If the total item count is zero and the previous isn't, assume the
            // list is invalidated and should be reset back to initial state
            if (totalItemCount < previousTotalItemCount)
            {
                this.currentPage = this.startingPageIndex;
                this.previousTotalItemCount = totalItemCount;
                if (totalItemCount == 0)
                {
                    this.loading = true;
                }
            }
            // If it’s still loading, we check to see if the dataset count has
            // changed, if so we conclude it has finished loading and update the current page
            // number and total item count.
            if (loading && (totalItemCount > previousTotalItemCount))
            {
                loading = false;
                previousTotalItemCount = totalItemCount;
            }

            // If it isn’t currently loading, we check to see if we have breached
            // the visibleThreshold and need to reload more data.
            // If we do need to reload some more data, we execute onLoadMore to fetch the data.
            // threshold should reflect how many total columns there are too
            if (!loading && (lastVisibleItemPosition + visibleThreshold) > totalItemCount)
            {
                currentPage++;
                OnLoadMore(currentPage, totalItemCount, recyclerView);
                loading = true;
            }
        }

        public void ResetState()
        {
            this.currentPage = this.startingPageIndex;
            this.previousTotalItemCount = 0;
            this.loading = true;
        }

        // Defines the process for actually loading more data based on page
        public abstract void OnLoadMore(int page, int totalItemsCount, RecyclerView view);
    }

    public class EndlessScroll : EndlessRecyclerViewScrollListener
    {
        public event EventHandler LoadMoreTask = delegate { };

        public EndlessScroll(LinearLayoutManager layoutManager) : base(layoutManager)
        {

        }

        public override void OnLoadMore(int page, int totalItemsCount, RecyclerView view)
        {
            LoadMoreTask?.Invoke(this, null); 
        }


    }
}