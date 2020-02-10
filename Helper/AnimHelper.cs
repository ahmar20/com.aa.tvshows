using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using AndroidX.Interpolator.View.Animation;

namespace com.aa.tvshows.Helper
{
    public static class AnimHelper
    {
        public static RotateAnimation InfiniteRotateAnimation => GetRotateAnimation();

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

        public static RotateAnimation GetRotateAnimation(float fromDegrees = 0, float toDegrees = 360,
            int repeatCount = RotateAnimation.Infinite, int startOffset = 1000)
        {
            RotateAnimation rotateAnim = new RotateAnimation(fromDegrees, toDegrees, 
                Dimension.RelativeToSelf, 0.5f, Dimension.RelativeToSelf, 0.5f);
            rotateAnim.Interpolator = new LinearInterpolator();
            rotateAnim.RepeatCount = repeatCount;
            rotateAnim.StartOffset = startOffset;
            rotateAnim.Duration = 1000;
            return rotateAnim;
        }

        public static void SetAlphaAnimation(View view, float fromAlpha = 0, float toAlpha = 1,
            int repeatCount = ValueAnimator.Infinite, int startOffset = 1000)
        {
            var anim = ObjectAnimator.OfFloat(view, "alpha", 1, 0);
            anim.SetDuration(1000);
            anim.StartDelay = startOffset;
            anim.RepeatMode = ValueAnimatorRepeatMode.Reverse;
            anim.RepeatCount = repeatCount;
            anim.Start();
        }
    }

}