using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;

namespace com.aa.tvshows.Fragments
{
    /// <summary>What airs next, grouped by day.</summary>
    public class ScheduleFragment : Fragment
    {
        RecyclerView list;
        ProgressBar progress;
        LinearLayoutCompat empty;
        int version;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_list, container, false);
            WindowInsetsHelper.Apply(view.FindViewById<View>(Resource.Id.list_root), top: true);

            view.FindViewById<TextView>(Resource.Id.list_title).Text = GetString(Resource.String.schedule_title);
            view.FindViewById<TextView>(Resource.Id.list_subtitle).Text = GetString(Resource.String.schedule_subtitle);
            list = view.FindViewById<RecyclerView>(Resource.Id.list_rv);
            progress = view.FindViewById<ProgressBar>(Resource.Id.list_progress);
            empty = view.FindViewById<LinearLayoutCompat>(Resource.Id.list_empty);
            list.SetLayoutManager(new LinearLayoutManager(Context));

            _ = LoadAsync();
            return view;
        }

        public override void OnDestroyView()
        {
            version++;
            base.OnDestroyView();
        }

        async Task LoadAsync()
        {
            var current = ++version;
            AppView.SetEmptyView(empty, false);
            progress.Visibility = ViewStates.Visible;

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<CalenderScheduleList>> days = null;
            try
            {
                days = await TMDbClient.GetTVSchedule();
            }
            catch (Exception)
            {
            }
            if (current != version || View == null) return;

            progress.Visibility = ViewStates.Gone;
            if (days == null || days.Count == 0)
            {
                AppView.SetEmptyView(empty, true, false, () => _ = LoadAsync());
                return;
            }

            var adapter = new ScheduleAdapter(days);
            adapter.ItemClick += item => AppView.HandleItemShowEpisodeClick(item, Context);
            list.SetAdapter(adapter);
        }
    }
}
