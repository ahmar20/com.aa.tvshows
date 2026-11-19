using Android.App;
using Android.Content;
using Android.Views;
using AndroidX.Core.View;

namespace com.aa.tvshows.Helper
{
    /// <summary>
    /// Apps targeting recent Android versions draw edge to edge (behind the status and navigation bars).
    /// These helpers keep content clear of the bars while letting artwork run underneath them.
    /// </summary>
    public static class WindowInsetsHelper
    {
        public static void EnableEdgeToEdge(Activity activity)
        {
            WindowCompat.SetDecorFitsSystemWindows(activity.Window, false);
        }

        /// <summary>Adds the system bar sizes to the view's own padding on the chosen sides.</summary>
        public static void Apply(View view, bool top = false, bool bottom = false, bool sides = false)
        {
            if (view == null) return;
            ViewCompat.SetOnApplyWindowInsetsListener(view, new SystemBarsInsetsListener(view, top, bottom, sides));
            ViewCompat.RequestApplyInsets(view);
        }

        /// <summary>Height of the status bar, for floating controls positioned with margins.</summary>
        public static int StatusBarHeight(Context context)
        {
            var id = context.Resources.GetIdentifier("status_bar_height", "dimen", "android");
            return id > 0 ? context.Resources.GetDimensionPixelSize(id) : 0;
        }
    }

    // Top-level (not nested) so the Android callable wrapper is generated normally.
    public class SystemBarsInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        readonly bool top, bottom, sides;
        readonly int baseLeft, baseTop, baseRight, baseBottom;

        public SystemBarsInsetsListener(View view, bool top, bool bottom, bool sides)
        {
            this.top = top;
            this.bottom = bottom;
            this.sides = sides;
            baseLeft = view.PaddingLeft;
            baseTop = view.PaddingTop;
            baseRight = view.PaddingRight;
            baseBottom = view.PaddingBottom;
        }

        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());
            v.SetPadding(
                baseLeft + (sides ? bars.Left : 0),
                baseTop + (top ? bars.Top : 0),
                baseRight + (sides ? bars.Right : 0),
                baseBottom + (bottom ? bars.Bottom : 0));
            return insets;
        }
    }
}
