using System;
using System.Collections.Generic;
using System.Globalization;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Newtonsoft.Json;

namespace com.aa.tvshows.Helper
{
    /// <summary>The episodes of the selected season on the show page. Tapping a row expands it in
    /// place to show the full overview and a Play button, rather than pushing a separate episode
    /// detail screen -- TMDB has no streams, so that screen never had anything to offer beyond the
    /// same overview/air-date text already shown here. Play fetches its source from
    /// EpisodeSourceProvider (a placeholder today) and hands off to PlayerActivity's ExoPlayer
    /// setup, or to the system player chooser if that's the user's preference.</summary>
    public class EpisodeRowsAdapter : RecyclerView.Adapter
    {
        List<ShowEpisodeDetails> items = new List<ShowEpisodeDetails>();
        // Keyed by EpisodeLink (stable identity) rather than position, since position changes
        // when the "latest first" setting flips the sort order.
        readonly HashSet<string> expanded = new HashSet<string>();
        readonly HashSet<string> loadingSource = new HashSet<string>();

        public override int ItemCount => items.Count;

        public void SetItems(IEnumerable<ShowEpisodeDetails> episodes)
        {
            items = new List<ShowEpisodeDetails>(episodes ?? new List<ShowEpisodeDetails>());
            NotifyDataSetChanged();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_episode, parent, false);
            return new EpisodeRowHolder(view,
                position =>
                {
                    if (position < 0 || position >= items.Count) return;
                    var key = items[position].EpisodeLink;
                    if (!expanded.Remove(key)) expanded.Add(key);
                    NotifyItemChanged(position);
                },
                (position, context) => PlayEpisode(position, context));
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

            // Expanded: full overview (no 3-line clip) plus the Play button.
            var isExpanded = expanded.Contains(episode.EpisodeLink);
            row.Overview.SetMaxLines(isExpanded ? int.MaxValue : 3);
            row.PlayButton.Visibility = isExpanded ? ViewStates.Visible : ViewStates.Gone;
            row.PlayButton.Enabled = !loadingSource.Contains(episode.EpisodeLink);
            row.PlayButton.Text = loadingSource.Contains(episode.EpisodeLink)
                ? row.PlayButton.Resources.GetString(Resource.String.action_loading)
                : row.PlayButton.Resources.GetString(Resource.String.action_play);

            AppView.LoadPicture(row.Thumb, episode.EpisodeImage);
            // no still yet (unaired): show the episode number in its place
            row.Number.Text = number;
            row.Number.Visibility = string.IsNullOrEmpty(episode.EpisodeImage) ? ViewStates.Visible : ViewStates.Gone;
        }

        async void PlayEpisode(int position, Context context)
        {
            if (position < 0 || position >= items.Count) return;
            var episode = items[position];
            if (loadingSource.Contains(episode.EpisodeLink)) return;

            loadingSource.Add(episode.EpisodeLink);
            NotifyItemChanged(position);

            List<StreamingUri> streams;
            try
            {
                streams = await EpisodeSourceProvider.GetStreamsAsync(episode);
            }
            finally
            {
                loadingSource.Remove(episode.EpisodeLink);
                var current = items.IndexOf(episode);
                if (current >= 0) NotifyItemChanged(current);
            }

            if (streams == null || streams.Count == 0)
            {
                Error.Instance.ShowErrorTip("No playback source is available for this episode yet.", context);
                return;
            }

            if (StorageData.GetUseExternalMediaPlayerSetting())
            {
                var viewIntent = new Intent(Intent.ActionView);
                viewIntent.SetDataAndType(Android.Net.Uri.Parse(streams[0].StreamingUrl.OriginalString), "video/*");
                context.StartActivity(Intent.CreateChooser(viewIntent, "Open video using"));
            }
            else
            {
                var intent = new Intent(context, typeof(PlayerActivity));
                intent.PutExtra("mediaStreams", JsonConvert.SerializeObject(streams));
                context.StartActivity(intent);
            }
        }
    }

    public class EpisodeRowHolder : RecyclerView.ViewHolder
    {
        public ImageView Thumb { get; }
        public TextView Number { get; }
        public TextView Title { get; }
        public TextView Meta { get; }
        public TextView Overview { get; }
        public Button PlayButton { get; }

        public EpisodeRowHolder(View itemView, Action<int> toggleExpand, Action<int, Context> play) : base(itemView)
        {
            Thumb = itemView.FindViewById<ImageView>(Resource.Id.ep_thumb);
            Number = itemView.FindViewById<TextView>(Resource.Id.ep_number);
            Title = itemView.FindViewById<TextView>(Resource.Id.ep_title);
            Meta = itemView.FindViewById<TextView>(Resource.Id.ep_meta);
            Overview = itemView.FindViewById<TextView>(Resource.Id.ep_overview);
            PlayButton = itemView.FindViewById<Button>(Resource.Id.ep_play_btn);

            itemView.Click += (s, e) =>
            {
                var position = AdapterPosition;
                if (position >= 0) toggleExpand(position);
            };
            PlayButton.Click += (s, e) =>
            {
                var position = AdapterPosition;
                if (position >= 0) play(position, itemView.Context);
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
