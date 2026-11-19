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

        string lastTipMessage;
        long lastTipTicks;

        public void ShowErrorTip(string message, Context c, ToastLength length = ToastLength.Short)
        {
            // several requests failing together (e.g. offline home screen) would otherwise queue a toast each
            var now = System.Environment.TickCount64;
            lock (this)
            {
                if (message == lastTipMessage && now - lastTipTicks < 4000) return;
                lastTipMessage = message;
                lastTipTicks = now;
            }

            // network code reports from background threads, and Toast needs the main looper
            new Handler(Looper.MainLooper).Post(() => Toast.MakeText(c, message, length).Show());
        }

        public void ShowErrorSnack(string message, View v, Action actionCallback = default, string actionName = default, int duration = Snackbar.LengthLong)
        {
            var snack = Snackbar.Make(v, message, duration);
            snack.SetAction("Dismiss", (s) => { snack.Dismiss(); }).SetActionTextColor(ContextCompat.GetColor(v.Context, Resource.Color.colorPrimary));
            if (actionCallback != null && !string.IsNullOrEmpty(actionName))
            {
                snack.SetAction(actionName, (s) => actionCallback.Invoke());
            }
            snack.Show();
        }
    }
}