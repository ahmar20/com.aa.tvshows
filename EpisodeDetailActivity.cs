using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.aa.tvshows
{
    /// <summary>
    /// An episode's page: still, synopsis and (when a source is configured) the list of stream hosts.
    /// TMDB has no streams, so by default this page shows the details and says so.
    /// </summary>
    [Activity(Label = "TV Episode Detail", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class EpisodeDetailActivity : AppCompatActivity
    {
        ImageView image;
        TextView showTitle, title, meta, overview, showButton, sourcesTitle, message;
        RecyclerView links;
        ProgressBar progress;

        ShowEpisodeDetails episode;
        bool canGoBackToSeriesHome;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            WindowInsetsHelper.EnableEdgeToEdge(this);
            SetContentView(Resource.Layout.activity_episode_detail);

            image = FindViewById<ImageView>(Resource.Id.ep_image);
            showTitle = FindViewById<TextView>(Resource.Id.ep_show_title);
            title = FindViewById<TextView>(Resource.Id.ep_title);
            meta = FindViewById<TextView>(Resource.Id.ep_meta);
            overview = FindViewById<TextView>(Resource.Id.ep_overview);
            showButton = FindViewById<TextView>(Resource.Id.ep_btn_show);
            sourcesTitle = FindViewById<TextView>(Resource.Id.ep_sources_title);
            message = FindViewById<TextView>(Resource.Id.ep_message);
            links = FindViewById<RecyclerView>(Resource.Id.ep_links);
            progress = FindViewById<ProgressBar>(Resource.Id.ep_progress);

            var back = FindViewById<View>(Resource.Id.ep_back);
            if (back != null)
            {
                var lp = (ViewGroup.MarginLayoutParams)back.LayoutParameters;
                lp.TopMargin += WindowInsetsHelper.StatusBarHeight(this);
                back.LayoutParameters = lp;
                back.Click += (s, e) => Finish();
            }

            links.SetLayoutManager(new LinearLayoutManager(this));
            showButton.Click += (s, e) =>
            {
                if (episode?.EpisodeShowLink == null) return;
                var intent = new Intent(this, typeof(ShowDetailActivity));
                intent.PutExtra("itemLink", episode.EpisodeShowLink);
                StartActivity(intent);
            };

            canGoBackToSeriesHome = Intent.GetBooleanExtra("canGoBackToSeriesHome", false);
            _ = LoadAsync(Intent.GetStringExtra("itemLink"));
        }

        async Task LoadAsync(string link)
        {
            progress.Visibility = ViewStates.Visible;
            try
            {
                episode = await TMDbClient.GetDetailsForTVShowEpisode(link);
            }
            catch (Exception)
            {
                episode = null;
            }
            if (IsDestroyed) return;
            progress.Visibility = ViewStates.Gone;

            if (episode == null)
            {
                title.Text = GetString(Resource.String.home_load_error);
                Error.Instance.ShowErrorTip("Episode data could not be loaded.", this);
                return;
            }

            AppView.LoadPicture(image, !string.IsNullOrEmpty(episode.EpisodeImageLarge) ? episode.EpisodeImageLarge : episode.EpisodeImage);
            showTitle.Text = episode.EpisodeShowTitle;
            title.Text = episode.EpisodeFullNameNumber;

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(episode.EpisodeNumber)) parts.Add(episode.EpisodeNumber);
            if (!string.IsNullOrEmpty(episode.EpisodeAirDate) && episode.EpisodeAirDate != "Unknown") parts.Add(DateText.Short(episode.EpisodeAirDate));
            if (!string.IsNullOrEmpty(episode.EpisodeRuntime)) parts.Add(episode.EpisodeRuntime);
            meta.Text = string.Join("  •  ", parts);

            overview.Text = string.IsNullOrWhiteSpace(episode.EpisodeSummary) ? GetString(Resource.String.detail_overview_missing) : episode.EpisodeSummary;
            // came here from the show's own page? then "Go to show" would just stack the same page again
            showButton.Visibility = canGoBackToSeriesHome || string.IsNullOrEmpty(episode.EpisodeShowLink) ? ViewStates.Gone : ViewStates.Visible;

            if (episode.IsEpisodeWatchable && episode.EpisodeStreamLinks != null && episode.EpisodeStreamLinks.Count > 0)
            {
                var streamLinks = episode.EpisodeStreamLinks;
                if (int.TryParse(StorageData.GetNumberOfVideoLinksSetting(), out int numberOfLinks) && numberOfLinks > 0)
                {
                    streamLinks = streamLinks.Take(numberOfLinks).ToList();
                }
                var adapter = new EpisodesAdapter<EpisodeStreamLink>(streamLinks);
                adapter.ItemClick += (s, position) =>
                {
                    var host = adapter.GetItem(position);
                    LoadingViewDialogForWebView.Instance.ShowDialog(this);
                    LoadingViewDialogForWebView.Instance.LoadUrl(host.HostUrl, host.HostName);
                };
                links.SetAdapter(adapter);
                sourcesTitle.Visibility = ViewStates.Visible;
            }
            else
            {
                message.Text = GetString(Resource.String.episode_no_sources);
                message.Visibility = ViewStates.Visible;
            }
            (showButton.Visibility == ViewStates.Visible ? showButton : (View)title).RequestFocus();
        }
    }
}
