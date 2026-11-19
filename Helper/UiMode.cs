using Android.Content;

namespace com.aa.tvshows.Helper
{
    /// <summary>
    /// Phone vs Android TV. The answer comes from the resource qualifier (values-television/bools.xml),
    /// so the same check that picks the TV layouts and dimensions also picks the TV behaviour.
    /// </summary>
    public static class UiMode
    {
        public static bool IsTv(Context context) => context.Resources.GetBoolean(Resource.Boolean.is_tv);
    }
}
