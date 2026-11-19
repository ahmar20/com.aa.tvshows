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
    /// <summary>What airs next (and recently), grouped by day. The user picks how many days back
    /// and forward to look; the choice is remembered for next time.</summary>
    public class ScheduleFragment : Fragment
    {
        const int MinDaysBack = 0, MaxDaysBack = 14;
        const int MinDaysForward = 1, MaxDaysForward = 21;

        RecyclerView list;
        ProgressBar progress;
        LinearLayoutCompat empty;
        TextView backLabel, forwardLabel;
        int version;
        int daysBack, daysForward;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_schedule, container, false);
            WindowInsetsHelper.Apply(view.FindViewById<View>(Resource.Id.list_root), top: true);

            view.FindViewById<TextView>(Resource.Id.list_title).Text = GetString(Resource.String.schedule_title);
            view.FindViewById<TextView>(Resource.Id.list_subtitle).Text = GetString(Resource.String.schedule_subtitle);
            list = view.FindViewById<RecyclerView>(Resource.Id.list_rv);
            progress = view.FindViewById<ProgressBar>(Resource.Id.list_progress);
            empty = view.FindViewById<LinearLayoutCompat>(Resource.Id.list_empty);
            list.SetLayoutManager(new LinearLayoutManager(Context));

            backLabel = view.FindViewById<TextView>(Resource.Id.schedule_back_label);
            forwardLabel = view.FindViewById<TextView>(Resource.Id.schedule_forward_label);
            daysBack = StorageData.GetScheduleDaysBack();
            daysForward = StorageData.GetScheduleDaysForward();
            UpdateLabels();

            view.FindViewById<View>(Resource.Id.schedule_back_minus).Click += (s, e) => ChangeDaysBack(-1);
            view.FindViewById<View>(Resource.Id.schedule_back_plus).Click += (s, e) => ChangeDaysBack(1);
            view.FindViewById<View>(Resource.Id.schedule_forward_minus).Click += (s, e) => ChangeDaysForward(-1);
            view.FindViewById<View>(Resource.Id.schedule_forward_plus).Click += (s, e) => ChangeDaysForward(1);

            _ = LoadAsync();
            return view;
        }

        public override void OnDestroyView()
        {
            version++;
            base.OnDestroyView();
        }

        void ChangeDaysBack(int delta)
        {
            var next = Math.Max(MinDaysBack, Math.Min(MaxDaysBack, daysBack + delta));
            if (next == daysBack) return;
            daysBack = next;
            StorageData.SetScheduleDaysBack(daysBack);
            UpdateLabels();
            _ = LoadAsync();
        }

        void ChangeDaysForward(int delta)
        {
            var next = Math.Max(MinDaysForward, Math.Min(MaxDaysForward, daysForward + delta));
            if (next == daysForward) return;
            daysForward = next;
            StorageData.SetScheduleDaysForward(daysForward);
            UpdateLabels();
            _ = LoadAsync();
        }

        void UpdateLabels()
        {
            backLabel.Text = daysBack == 0 ? "No past days" : $"Past {daysBack}d";
            forwardLabel.Text = $"Next {daysForward}d";
        }

        async Task LoadAsync()
        {
            var current = ++version;
            AppView.SetEmptyView(empty, false);
            list.Visibility = ViewStates.Invisible;
            progress.Visibility = ViewStates.Visible;

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<CalenderScheduleList>> days = null;
            try
            {
                days = await TMDbClient.GetTVSchedule(daysBack, daysForward);
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
            list.Visibility = ViewStates.Visible;
        }
    }
}
