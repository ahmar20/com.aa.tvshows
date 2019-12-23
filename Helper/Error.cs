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
        private static Error instance;
        public static Error Instance
        {
            get
            {
                if (instance == null) instance = new Error();
                return instance;
            }
        }

        public Error()
        {
            instance = new Error();
        }

        public void ShowErrorTip(string message, Context c)
        {
            Toast.MakeText(c, message, ToastLength.Short).Show();
        }

        public void ShowErrorSnack(string message, View v, Action actionCallback = default)
        {
            Snackbar.Make(v, message, Snackbar.LengthIndefinite)
                    .SetAction("OK", (s) => { actionCallback?.Invoke(); })
                    .SetActionTextColor(ContextCompat.GetColor(v.Context, Resource.Color.colorPrimary))
                    .Show();
        }
    }
}