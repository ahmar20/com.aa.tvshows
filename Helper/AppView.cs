using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using com.aa.tvshows.Models;
using Square.Picasso;
using System;
using System.Threading.Tasks;

namespace com.aa.tvshows.Helper
{
    public static class AppView
    {
        /// <summary>Hooks a flat toolbar up as the support action bar with an up arrow (Settings, About, Genres).</summary>
        public static void SetActionBarForActivity(AndroidX.AppCompat.Widget.Toolbar toolbar, AppCompatActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            if (toolbar == null) throw new ArgumentNullException(nameof(toolbar));

            activity.SetSupportActionBar(toolbar);
            activity.SupportActionBar.SetDisplayShowHomeEnabled(true);
            activity.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
        }

        public static void HandleItemShowEpisodeClick(object item, Context context)
        {
            var pageLink = string.Empty;
            var canGoBackToSeriesHome = false;
            if (item is MediaCard card)
            {
                pageLink = card.PageLink;
            }
            else if (item is CalenderScheduleList calenderItem)
            {
                pageLink = calenderItem.PageLink;
            }
            else if (item is ShowList showItem)
            {
                pageLink = showItem.PageLink;
            }
            else if (item is SearchList searchItem)
            {
                pageLink = searchItem.PageLink;
            }
            else if (item is EpisodeList episodeItem)
            {
                pageLink = episodeItem.PageLink;
            }
            else if (item is SearchSuggestionsData searchSuggestion)
            {
                pageLink = searchSuggestion.ItemLink;
            }
            else if (item is GenresShow genreShow)
            {
                pageLink = genreShow.PageLink;
            }
            else if (item is ShowEpisodeDetails episodeDetailLink)
            {
                pageLink = episodeDetailLink.EpisodeLink;
                canGoBackToSeriesHome = true;
            }
            else if (item is SeriesDetails seriesDetailLink)
            {
                pageLink = seriesDetailLink.SeriesLink;
            }

            if (string.IsNullOrEmpty(pageLink))
            {
                Error.Instance.ShowErrorTip("Link is empty.", context);
                return;
            }
            if (TMDbClient.IsEpisodeLink(pageLink))
            {
                // tv episode detail
                var intent = new Intent(context, typeof(EpisodeDetailActivity));
                intent.PutExtra("itemLink", pageLink);
                intent.PutExtra("canGoBackToSeriesHome", canGoBackToSeriesHome);
                StartActivityWithTransition(context, intent);
            }
            else
            {
                // tv show detail
                var intent = new Intent(context, typeof(ShowDetailActivity));
                intent.PutExtra("itemLink", pageLink);
                StartActivityWithTransition(context, intent);
            }
        }

        /// <summary>Opens a new screen with a slide/fade instead of the default hard cut.</summary>
        public static void StartActivityWithTransition(Context context, Intent intent)
        {
            context.StartActivity(intent);
            (context as Activity)?.OverridePendingTransition(Resource.Animation.anim_enter, Resource.Animation.anim_exit);
        }

        /// <summary>Call instead of Finish() on a pushed screen so the back transition matches the forward one.</summary>
        public static void FinishWithTransition(Activity activity)
        {
            activity.Finish();
            activity.OverridePendingTransition(Resource.Animation.anim_pop_enter, Resource.Animation.anim_pop_exit);
        }

        /// <summary>
        /// Empty / error state. With <paramref name="isFavorite"/> it is the "My List is empty" message;
        /// otherwise it is the connection error. The retry button only shows when there is something to retry.
        /// </summary>
        public static void SetEmptyView(LinearLayoutCompat view, bool show, bool isFavorite = false, Action retryAction = default)
        {
            if (view is null) return;

            view.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
            view.RemoveAllViews();
            if (!show)
            {
                return;
            }
            var context = view.Context;
            var parent = LayoutInflater.From(view.Context).Inflate(Resource.Layout.empty_view, null, false);
            // inflating with a null root loses the proper LayoutParams subtype for this parent, which
            // otherwise left the icon+text shrink-wrapped into a narrow column instead of spanning the screen
            parent.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            var emptyHeader = parent.FindViewById<AppCompatTextView>(Resource.Id.empty_view_header);
            var emptyContent = parent.FindViewById<AppCompatTextView>(Resource.Id.empty_view_content);
            var emptyImage = parent.FindViewById<AppCompatImageView>(Resource.Id.empty_view_tagline_image);
            var emptyRetryBtn = parent.FindViewById<AppCompatButton>(Resource.Id.empty_view_retry_btn);
            if (isFavorite)
            {
                emptyHeader.Text = context.Resources.GetString(Resource.String.empty_favorites_header);
                emptyContent.Text = context.Resources.GetString(Resource.String.empty_favorites_content);
                emptyImage.SetImageDrawable(ContextCompat.GetDrawable(parent.Context, Resource.Drawable.baseline_favorite_24));
            }
            else
            {
                emptyHeader.Text = context.Resources.GetString(Resource.String.internet_error_header);
                emptyContent.Text = context.Resources.GetString(Resource.String.internet_error_content);
                emptyImage.SetImageDrawable(ContextCompat.GetDrawable(parent.Context, Resource.Drawable.sharp_error_outline_24));
            }

            if (retryAction == null)
            {
                emptyRetryBtn.Visibility = ViewStates.Gone;
            }
            else
            {
                emptyRetryBtn.Click += delegate { view.RemoveAllViews(); retryAction.Invoke(); };
            }
            if (!isFavorite) AnimHelper.SetAlphaAnimation(emptyImage);
            view.AddView(parent);
        }

        public static void ShowLoadingView(View view, bool show)
        {
            if (view is null) return;
            if (view is SwipeRefreshLayout swipe)
            {
                if (swipe.Refreshing && show) return;
                swipe.Enabled = !show;
                swipe.Refreshing = show;
            }
            else if (view is ContentLoadingProgressBar prog)
            {
                prog.Indeterminate = true;
                prog.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
            }
            else
            {
                view.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
            }
        }

        /// <summary>
        /// Loads artwork into a tile, hero or thumbnail. The view's own background (the dark card shape)
        /// is the placeholder, so tiles never flash a loading image; a missing url just leaves it blank.
        /// </summary>
        public static void LoadPicture(ImageView imageView, string url)
        {
            if (imageView == null) return;

            var picasso = Picasso.Get();
            picasso.CancelRequest(imageView);
            if (string.IsNullOrEmpty(url))
            {
                imageView.SetImageDrawable(null);
                return;
            }
            picasso.Load(url)
                .Fit()
                .CenterCrop()
                .Into(imageView);
        }

        public static async void LoadImageIntoView<T>(T imageLink, AppCompatImageView imageView)
        {
            Picasso picassoContext = Picasso.Get();
            RequestCreator picassoRequest = null;
            if (imageLink is string link)
            {
                picassoRequest = picassoContext.Load(link);
            }
            else if (imageLink is Task<string> linkTask)
            {
                string imageUri = await linkTask;
                picassoRequest = picassoContext.Load(string.IsNullOrEmpty(imageUri) ? "com.aa.tvshows" : imageUri);
            }
            else
            {
                throw new Exception("invalid image link");
            }
            picassoRequest.Error(Resource.Drawable.no_image)
                .Placeholder(Resource.Drawable.loading)
                .Into(imageView);
        }
    }
}
