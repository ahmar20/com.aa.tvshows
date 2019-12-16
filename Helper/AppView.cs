using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;

namespace com.aa.tvshows.Helper
{
    public static class AppView
    {
        const int TVScheduleId = 100;
        const int SearchId = 101;
        public const int ReloadId = 102;
        const int GenresId = 103;
        const int SettingsId = 110;
        const int AboutId = 111;
        const int mainItemsGroupId = 200;
        const int appItemsGroupId = 201;

        public static void SetActionBarForActivity(Toolbar toolbar, AppCompatActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            if (toolbar == null) throw new ArgumentNullException(nameof(toolbar));

            activity.SetSupportActionBar(toolbar);
            if (activity.LocalClassName.ToUpperInvariant().Contains("MAIN", StringComparison.Ordinal))
            {
                activity.SupportActionBar.Title = activity.Resources.GetString(Resource.String.app_name);
                activity.SupportActionBar.Subtitle = "Watch TV Shows of your choice";
                activity.SupportActionBar.SetDisplayShowHomeEnabled(true);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("TVSCHEDULE", StringComparison.Ordinal))
            {
                activity.SupportActionBar.Title = "TV Shows Schedule";
                activity.SupportActionBar.Subtitle = "See what's missed and what's next!";
                activity.SupportActionBar.SetDisplayShowHomeEnabled(true);
                activity.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("GENRES", StringComparison.Ordinal))
            {
                activity.SupportActionBar.Title = "TV Shows Genres";
                activity.SupportActionBar.Subtitle = "Browse all types of shows";
                activity.SupportActionBar.SetDisplayShowHomeEnabled(true);
                activity.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            }
        }

        public static bool ShowOptionsMenu(IMenu menu, AppCompatActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            int itemsOrder = 0;
            if (activity.LocalClassName.ToUpperInvariant().Contains("MAIN", StringComparison.Ordinal))
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
            else if (activity.LocalClassName.ToUpperInvariant().Contains("TVSCHEDULE", StringComparison.Ordinal))
            {
                menu.Add(mainItemsGroupId, ReloadId, itemsOrder++, "Reload Data")
                    .SetIcon(Resource.Drawable.baseline_refresh_24)
                    .SetShowAsAction(ShowAsAction.IfRoom);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("GENRES", StringComparison.Ordinal))
            {
                menu.Add(mainItemsGroupId, ReloadId, itemsOrder++, "Reload Data")
                    .SetIcon(Resource.Drawable.baseline_refresh_24)
                    .SetShowAsAction(ShowAsAction.IfRoom);
            }
            menu.Add(appItemsGroupId, SettingsId, itemsOrder++, "Settings").SetShowAsAction(ShowAsAction.Never);
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
                    //activity.StartActivity(typeof(SearchActivity));
                    break;

                case GenresId:
                    activity.StartActivity(typeof(GenresActivity));
                    break;

                default:
                    return false;
            }
            return true;
        }
    }
}