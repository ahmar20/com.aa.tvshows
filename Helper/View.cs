using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
//using AndroidX.AppCompat.App;
//using AndroidX.AppCompat.Widget;

namespace com.aa.tvshows.Helper
{
    public static class AppView
    {
        public static void SetActionBarForActivity(Toolbar toolbar, AppCompatActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            if (toolbar == null) throw new ArgumentNullException(nameof(toolbar));

            activity.SetSupportActionBar(toolbar);
            if (activity.LocalClassName.ToUpperInvariant().Contains("MAIN", StringComparison.Ordinal))
            {
                activity.SupportActionBar.Title = activity.Resources.GetString(Resource.String.app_name);
                activity.SupportActionBar.Subtitle = "New and Popular TV Shows";
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("TVSCHEDULE", StringComparison.Ordinal))
            {
                activity.SupportActionBar.Title = "TV Shows Schedule";
                activity.SupportActionBar.Subtitle = null;
            }
        }

        public static bool ShowOptionsMenu(IMenu menu, AppCompatActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            if (menu == null) throw new ArgumentNullException(nameof(menu));

            if (activity.LocalClassName.ToUpperInvariant().Contains("MAIN", StringComparison.Ordinal))
            {
                using var scheduleIntent = new Intent(activity, typeof(TVScheduleActivity));
                menu.Add("TV Shows Schedule")
                    .SetIcon(Android.Resource.Drawable.IcMenuMyCalendar)
                    .SetIntent(scheduleIntent)
                    .SetShowAsAction(ShowAsAction.Always);

                menu.Add("Settings").SetShowAsAction(ShowAsAction.Never);
                menu.Add("About").SetShowAsAction(ShowAsAction.Never);
            }
            else if (activity.LocalClassName.ToUpperInvariant().Contains("TVSCHEDULE", StringComparison.Ordinal))
            {
                menu.Add("Settings").SetShowAsAction(ShowAsAction.Never);
                menu.Add("About").SetShowAsAction(ShowAsAction.Never);
            }

            return true;
        }
    }
}