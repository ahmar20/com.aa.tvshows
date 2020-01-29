using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Interpolator.View.Animation;

namespace com.aa.tvshows.Helper
{
    public static class AnimHelper
    {
        public static FastOutSlowInInterpolator FastOutSlowInInterpolator = new FastOutSlowInInterpolator();
        public static void FadeContents(View view, bool direction, bool animated, Action end)
        {
            if (view == null)
            {
                return;
            }

            float alpha = direction ? 0f : 1f;
            if (!direction)
            {
                view.ScaleX = 0.95f;
                view.ScaleY = 0.95f;
            }
            if (animated)
                view.Animate()
                    .Alpha(alpha)
                    .SetDuration(150)
                    .ScaleX(1f)
                    .ScaleY(1f)
                    .SetInterpolator(AnimHelper.FastOutSlowInInterpolator)
                    .WithEndAction(end == null ? null : new Java.Lang.Runnable(end))
                    .Start();
            else
            {
                view.Alpha = alpha;
                view.ScaleX = 1f;
                view.ScaleY = 1f;
            }
        }
    }

}