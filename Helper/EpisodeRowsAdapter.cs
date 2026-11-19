using System;
using System.Collections.Generic;
using System.Globalization;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace com.aa.tvshows.Helper
{
    /// <summary>The episodes of the selected season on the show page.</summary>
    public class EpisodeRowsAdapter : RecyclerView.Adapter
    {
        List<ShowEpisodeDetails> items = new List<ShowEpisodeDetails>();

        public event Action<ShowEpisodeDetails> ItemClick;

        public override int ItemCount => items.Count;

        public void SetItems(IEnumerable<ShowEpisodeDetails> episodes)
        {
            items = new List<ShowEpisodeDetails>(episodes ?? new List<ShowEpisodeDetails>());
            NotifyDataSetChanged();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_episode, parent, false);
            return new EpisodeRowHolder(view, position =>
            {
                if (position < items.Count) ItemClick?.Invoke(items[position]);
            });
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (!(holder is EpisodeRowHolder row)) return;

            var episode = items[position];
            var number = episode.EpisodeIndex > 0 ? episode.EpisodeIndex.ToString(CultureInfo.InvariantCulture) : string.Empty;
            row.Title.Text = string.IsNullOrEmpty(number) ? episode.EpisodeFullNameNumber : $"{number}. {episode.EpisodeFullNameNumber}";

            var meta = new List<string>();
            if (!string.IsNullOrEmpty(episode.EpisodeAirDate) && episode.EpisodeAirDate != "Unknown")
            {
                meta.Add(DateText.Short(episode.EpisodeAirDate));
            }
            if (!string.IsNullOrEmpty(episode.EpisodeRuntime)) meta.Add(episode.EpisodeRuntime);
            row.Meta.Text = string.Join("  •  ", meta);
            row.Meta.Visibility = meta.Count == 0 ? ViewStates.Gone : ViewStates.Visible;

            row.Overview.Text = episode.EpisodeSummary;
            row.Overview.Visibility = string.IsNullOrWhiteSpace(episode.EpisodeSummary) ? ViewStates.Gone : ViewStates.Visible;

            AppView.LoadPicture(row.Thumb, episode.EpisodeImage);
            // no still yet (unaired): show the episode number in its place
            row.Number.Text = number;
            row.Number.Visibility = string.IsNullOrEmpty(episode.EpisodeImage) ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    public class EpisodeRowHolder : RecyclerView.ViewHolder
    {
        public ImageView Thumb { get; }
        public TextView Number { get; }
        public TextView Title { get; }
        public TextView Meta { get; }
        public TextView Overview { get; }

        public EpisodeRowHolder(View itemView, Action<int> click) : base(itemView)
        {
            Thumb = itemView.FindViewById<ImageView>(Resource.Id.ep_thumb);
            Number = itemView.FindViewById<TextView>(Resource.Id.ep_number);
            Title = itemView.FindViewById<TextView>(Resource.Id.ep_title);
            Meta = itemView.FindViewById<TextView>(Resource.Id.ep_meta);
            Overview = itemView.FindViewById<TextView>(Resource.Id.ep_overview);
            itemView.Click += (s, e) =>
            {
                var position = AdapterPosition;
                if (position >= 0) click(position);
            };
        }
    }

    public static class DateText
    {
        /// <summary>"2026-11-19" becomes "Nov 19, 2026"; anything unparseable is returned as is.</summary>
        public static string Short(string isoDate)
        {
            return DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
                : isoDate;
        }
    }
}
