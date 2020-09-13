using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Renderscripts;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics.Drawable;
using AndroidX.Core.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Square.Picasso;
using System;
using System.Threading.Tasks;

namespace com.aa.tvshows.Helper
{
    public static class AppView
    {
        const int TVScheduleId = 100;
        const int SearchId = 101;
        public const int ReloadId = 102;
        const int GenresId = 103;
        public const int AddFavoritesId = 104;
        public const int RemoveFavoritesId = 105;
        public const int GoToSeriesHomeId = 106;
        const int SettingsId = 110;
        const int AboutId = 111;
        public const int mainItemsGroupId = 200;
        public const int appItemsGroupId = 201;

        public static void SetActionBarForActivity(Toolbar toolbar, AppCompatActivity activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (toolbar == null)
            {
                throw new ArgumentNullException(nameof(toolbar));
            }

            activity.SetSupportActionBar(toolbar);
            if (activity.LocalClassName.ToUpperInvariant().Contains("MAIN", StringComparison.InvariantCulture))
            {
                activity.SupportActionBar.Title = activity.Resources.GetString(Resource.String.app_name);
                activity.SupportActionBar.Subtitle = "Watch TV Shows of your choice";
            }
            else
            {
                activity.SupportActionBar.SetDisplayShowHomeEnabled(true);
                activity.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            }

            if (activity.LocalClassName.ToUpperInvariant().Contains("TVSCHEDULE", StringComparison.InvariantCulture))
            {
                activity.SupportActionBar.Title = "Schedule";
                activity.SupportActionBar.Subtitle = "See what's missed and what's next!";
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("GENRES", StringComparison.InvariantCulture))
            {
                activity.SupportActionBar.Title = "Genres";
                activity.SupportActionBar.Subtitle = "Browse all types of shows";
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("SEARCH", StringComparison.InvariantCulture))
            {
                activity.SupportActionBar.Title = "Search";
                activity.SupportActionBar.Subtitle = string.Empty;
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("SHOWDETAIL", StringComparison.InvariantCulture))
            {
            }
        }

        public static bool ShowOptionsMenu(IMenu menu, AppCompatActivity activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            int itemsOrder = 1;
            if (activity.LocalClassName.ToUpperInvariant().Contains("MAIN"))
            {
                menu.Add(mainItemsGroupId, SearchId, itemsOrder++, "Search")
                    .SetIcon(Resource.Drawable.baseline_search_24)
                    .SetShowAsAction(ShowAsAction.Always);
                menu.Add(mainItemsGroupId, TVScheduleId, itemsOrder++, "TV Shows Schedule")
                    .SetIcon(Resource.Drawable.baseline_date_range_24)
                    .SetShowAsAction(ShowAsAction.Always);
                menu.Add(mainItemsGroupId, GenresId, itemsOrder++, "Browse by Genres")
                    .SetIcon(Resource.Drawable.baseline_movie_24)
                    .SetShowAsAction(ShowAsAction.Always);

                menu.Add(appItemsGroupId, SettingsId, itemsOrder++, "Settings").SetShowAsAction(ShowAsAction.Never);
                menu.Add(appItemsGroupId, AboutId, itemsOrder++, "About").SetShowAsAction(ShowAsAction.Never);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("TVSCHEDULE"))
            {
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("GENRES"))
            {
            }

            return true;
        }

        public static bool OnOptionsItemSelected(IMenuItem item, AppCompatActivity activity, Action action = default)
        {
            if (action != null)
            {
                action.Invoke();
                return true;
            }
            switch (item.ItemId)
            {
                case TVScheduleId:
                    activity.StartActivity(typeof(TVScheduleActivity));
                    break;

                case SearchId:
                    activity.StartActivity(typeof(SearchActivity));
                    break;

                case GenresId:
                    activity.StartActivity(typeof(GenresActivity));
                    break;

                case AboutId:
                    activity.StartActivity(typeof(AboutActivity));
                    break;

                case SettingsId:
                    activity.StartActivity(typeof(SettingsActivity));
                    break;

                default:
                    return false;
            }
            return true;
        }

        public static void HandleItemShowEpisodeClick(object item, Context context)
        {
            var pageLink = string.Empty;
            var canGoBackToSeriesHome = false;
            if (item is CalenderScheduleList calenderItem)
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
            if (pageLink.Contains("/serie/"))
            {
                // tv show detail
                var intent = new Android.Content.Intent(context, typeof(ShowDetailActivity));
                intent.PutExtra("itemLink", pageLink);
                context.StartActivity(intent);
            }
            else
            {
                // tv episode detail
                var intent = new Android.Content.Intent(context, typeof(EpisodeDetailActivity));
                intent.PutExtra("itemLink", pageLink);
                intent.PutExtra("canGoBackToSeriesHome", canGoBackToSeriesHome);
                context.StartActivity(intent);
            }
        }

        public static void CreateBluredView(this View v, View toCapture, AppCompatActivity activity)
        {
            Bitmap bitmap = CaptureView(toCapture);
            if (bitmap != null)
            {
                BlurBitmapWithRenderscript(RenderScript.Create(activity), bitmap);
            }
            SetBackgroundOnView(v, bitmap, activity);
        }
        private static Bitmap CaptureView(View view)
        {
            //Create a Bitmap with the same dimensions as the View
            Bitmap image = Bitmap.CreateBitmap(view.MeasuredWidth, view.MeasuredHeight, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(image);
            view.Draw(canvas);

            //Make it frosty
            Paint paint = new Paint();
            paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.SrcIn));
            
            //ColorFilter filter = new LightingColorFilter(0xFFFFFFFF, 0x00222222); // lighten
            //ColorFilter filter = new LightingColorFilter(0xFF7F7F7F, 0x00000000); // darken

            //paint.SetColorFilter(filter);
            canvas.DrawBitmap(image, 0, 0, paint);
            return image;
        }

        public static void BlurBitmapWithRenderscript(RenderScript rs, Bitmap bitmap2)
        {
            // this will blur the bitmapOriginal with a radius of 25
            // and save it in bitmapOriginal
            // use this constructor for best performance, because it uses
            // USAGE_SHARED mode which reuses memory
            Allocation input = Allocation.CreateFromBitmap(rs, bitmap2);
            Allocation output = Allocation.CreateTyped(rs, input.Type);
            ScriptIntrinsicBlur script = ScriptIntrinsicBlur.Create(rs, Element.U8_4(rs));
            // must be >0 and <= 25
            script.SetRadius(25f);
            script.SetInput(input);
            script.ForEach(output);
            output.CopyTo(bitmap2);
        }

        private static void SetBackgroundOnView(View view, Bitmap bitmap, AppCompatActivity activity)
        {
            Drawable d = null;
            if (bitmap != null)
            {
                d = RoundedBitmapDrawableFactory.Create(activity.Resources, bitmap);
                ((RoundedBitmapDrawable)d).CornerRadius = 10;
            }
            view.Background = d;
        }

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
            var emptyHeader = parent.FindViewById<AppCompatTextView>(Resource.Id.empty_view_header);
            var emptyContent = parent.FindViewById<AppCompatTextView>(Resource.Id.empty_view_content);
            var emptyImage = parent.FindViewById<AppCompatImageView>(Resource.Id.empty_view_tagline_image);
            var emptyRetryBtn = parent.FindViewById<AppCompatButton>(Resource.Id.empty_view_retry_btn);
            if (isFavorite)
            {
                emptyHeader.Text = context.Resources.GetString(Resource.String.empty_favorites_header);
                emptyContent.Text = context.Resources.GetString(Resource.String.empty_favorites_content);
                emptyImage.SetImageDrawable(ContextCompat.GetDrawable(parent.Context, Resource.Drawable.baseline_favorite_24));
                emptyRetryBtn.Click += delegate { view.RemoveAllViews(); retryAction?.Invoke(); };
            }
            else
            {
                emptyHeader.Text = context.Resources.GetString(Resource.String.internet_error_header);
                emptyContent.Text = context.Resources.GetString(Resource.String.internet_error_content);
                emptyImage.SetImageDrawable(ContextCompat.GetDrawable(parent.Context, Resource.Drawable.sharp_error_outline_24));
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
        }

        public static void LoadImageIntoView(string imageLink, AppCompatImageView imageView)
        {
            Picasso.Get()
                .Load(imageLink)
                .Error(Resource.Drawable.no_image)
                .Placeholder(Resource.Drawable.loading)
                .Into(imageView);
        }
    }
}