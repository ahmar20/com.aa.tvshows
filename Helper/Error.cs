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
using AndroidX.Core.Content;
using Google.Android.Material.Snackbar;

namespace com.aa.tvshows.Helper
{
    public class Error
    {
        public static Error Instance { get; } = new Error();

        public void ShowErrorTip(string message, Context c)
        {
            Toast.MakeText(c, message, ToastLength.Short).Show();
        }

        public void ShowErrorSnack(string message, View v, Action actionCallback = default, string actionName = default)
        {
            var snack = Snackbar.Make(v, message, Snackbar.LengthLong)
                        .SetAction("Dismiss", (s) => { })
                        .SetActionTextColor(ContextCompat.GetColor(v.Context, Resource.Color.colorPrimary));
            if (actionCallback != null && !string.IsNullOrEmpty(actionName))
            {
                snack.SetAction(actionName, (s) => actionCallback.Invoke());
            }
            snack.Show();
        }
    }
}