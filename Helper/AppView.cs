using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Renderscripts;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Graphics.Drawable;
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
                activity.SupportActionBar.Title = "TV Shows Schedule";
                activity.SupportActionBar.Subtitle = "See what's missed and what's next!";
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("GENRES", StringComparison.InvariantCulture))
            {
                activity.SupportActionBar.Title = "TV Shows Genres";
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

        public static bool ShowOptionsMenu(IMenu menu, AppCompatActivity activity, bool isFavorite = false)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            int itemsOrder = 0;
            if (activity.LocalClassName.ToUpperInvariant().Contains("MAIN"))
            {
                menu.Add(mainItemsGroupId, SearchId, itemsOrder++, "Search")
                    .SetIcon(Resource.Drawable.baseline_search_24)
                    .SetShowAsAction(ShowAsAction.Always);
                menu.Add(mainItemsGroupId, TVScheduleId, itemsOrder++, "TV Shows Schedule")
                    .SetIcon(Resource.Drawable.baseline_date_range_24)
                    .SetShowAsAction(ShowAsAction.Always);
                menu.Add(mainItemsGroupId, GenresId, itemsOrder++, "Browse by Genres")
                    .SetIcon(Resource.Drawable.film)
                    .SetShowAsAction(ShowAsAction.IfRoom);
                menu.Add(mainItemsGroupId, ReloadId, itemsOrder++, "Reload Data")
                    .SetIcon(Resource.Drawable.baseline_refresh_24)
                    .SetShowAsAction(ShowAsAction.Never);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("TVSCHEDULE"))
            {
                menu.Add(mainItemsGroupId, ReloadId, itemsOrder++, "Reload Data")
                    .SetIcon(Resource.Drawable.baseline_refresh_24)
                    .SetShowAsAction(ShowAsAction.IfRoom);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("GENRES"))
            {
                menu.Add(mainItemsGroupId, ReloadId, itemsOrder++, "Reload Data")
                    .SetIcon(Resource.Drawable.baseline_refresh_24)
                    .SetShowAsAction(ShowAsAction.IfRoom);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("SHOWDETAIL"))
            {
                if (isFavorite)
                {
                    menu.Add(mainItemsGroupId, RemoveFavoritesId, itemsOrder++, "Remove Favorite")
                        .SetIcon(Resource.Drawable.baseline_favorite_24)
                        .SetShowAsAction(ShowAsAction.Always);
                }
                else
                {
                    menu.Add(mainItemsGroupId, AddFavoritesId, itemsOrder++, "Mark Favorite")
                        .SetIcon(Resource.Drawable.baseline_favorite_border_24)
                        .SetShowAsAction(ShowAsAction.Always);
                }
            }
            //menu.Add(appItemsGroupId, SettingsId, itemsOrder++, "Settings").SetShowAsAction(ShowAsAction.Never);
            menu.Add(appItemsGroupId, AboutId, itemsOrder++, "About").SetShowAsAction(ShowAsAction.Never);

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

                default:
                    return false;
            }
            return true;
        }

        public static void HandleItemShowEpisodeClick(object item, Context context)
        {
            var pageLink = string.Empty;
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
            Bitmap image = Bitmap.CreateBitmap(view.MeasuredWidth, view.MeasuredHeight, Bitmap.Config.Argb4444); 
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
    }
}