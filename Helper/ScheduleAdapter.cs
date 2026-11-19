using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace com.aa.tvshows.Helper
{
    /// <summary>Day headers with the episodes airing that day underneath.</summary>
    public class ScheduleAdapter : RecyclerView.Adapter
    {
        const int HeaderType = 0;
        const int EntryType = 1;

        readonly List<object> entries = new List<object>();

        public event Action<CalenderScheduleList> ItemClick;

        public ScheduleAdapter(Dictionary<string, List<CalenderScheduleList>> days)
        {
            foreach (var day in days)
            {
                entries.Add(day.Key);
                entries.AddRange(day.Value);
            }
        }

        public override int ItemCount => entries.Count;

        public override int GetItemViewType(int position) => entries[position] is string ? HeaderType : EntryType;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var inflater = LayoutInflater.From(parent.Context);
            if (viewType == HeaderType)
            {
                return new ScheduleHeaderHolder(inflater.Inflate(Resource.Layout.item_schedule_header, parent, false));
            }
            return new ScheduleEntryHolder(inflater.Inflate(Resource.Layout.item_schedule_entry, parent, false),
                position => { if (entries[position] is CalenderScheduleList item) ItemClick?.Invoke(item); });
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is ScheduleHeaderHolder header)
            {
                header.Text.Text = (string)entries[position];
            }
            else if (holder is ScheduleEntryHolder entry && entries[position] is CalenderScheduleList item)
            {
                entry.Title.Text = item.Title;
                entry.Episode.Text = item.EpisodeNo;
                entry.Name.Text = item.EpisodeName;
                entry.Name.Visibility = string.IsNullOrEmpty(item.EpisodeName) ? ViewStates.Gone : ViewStates.Visible;
                AppView.LoadPicture(entry.Image, item.ImageLink);
            }
        }
    }

    public class ScheduleHeaderHolder : RecyclerView.ViewHolder
    {
        public TextView Text { get; }

        public ScheduleHeaderHolder(View itemView) : base(itemView)
        {
            Text = itemView.FindViewById<TextView>(Resource.Id.header_text);
        }
    }

    public class ScheduleEntryHolder : RecyclerView.ViewHolder
    {
        public ImageView Image { get; }
        public TextView Title { get; }
        public TextView Episode { get; }
        public TextView Name { get; }

        public ScheduleEntryHolder(View itemView, Action<int> click) : base(itemView)
        {
            Image = itemView.FindViewById<ImageView>(Resource.Id.entry_image);
            Title = itemView.FindViewById<TextView>(Resource.Id.entry_title);
            Episode = itemView.FindViewById<TextView>(Resource.Id.entry_episode);
            Name = itemView.FindViewById<TextView>(Resource.Id.entry_name);
            itemView.Click += (s, e) =>
            {
                var position = AdapterPosition;
                if (position >= 0) click(position);
            };
        }
    }
}
