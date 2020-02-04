using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Widget;
using com.aa.tvshows.Helper;
using Newtonsoft.Json;
using System.Linq;
using Android.Views;

using Com.Google.Android.Exoplayer2.Source.Dash;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming;
using Com.Google.Android.Exoplayer2.Source.Dash.Manifest;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming.Manifest;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Offline;
using Com.Google.Android.Exoplayer2.Source.Hls;
using Com.Google.Android.Exoplayer2.Source.Hls.Playlist;
using Com.Google.Android.Exoplayer2.Util;
using Com.Google.Android.Exoplayer2.Trackselection;


namespace com.aa.tvshows
{
    [Activity(Label = "PlayerActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.SensorLandscape, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class PlayerActivity : AppCompatActivity, PlayerControlView.IVisibilityListener
    {
        SimpleExoPlayer player;
        PlayerView playerView;
        AppCompatSpinner linksSpinner;

        List<StreamingUri> mediaStreams;
        bool isSystemUIVisible = true;
        
        IDataSourceFactory mediaSourceFactory;
        ITrackSelectionFactory trackSelectorFactory;
        readonly IBandwidthMeter bandwidthMeter = new DefaultBandwidthMeter();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.player_view);
            Window.DecorView.SystemUiVisibilityChange += VideoPlayer_SystemUiVisibilityChange;
            InitializePlayback(null);
        }

        private void InitializePlayback(StreamingUri itemToPlay)
        {
            if (mediaStreams == null)
                mediaStreams = JsonConvert.DeserializeObject<List<StreamingUri>>(Intent.GetStringExtra("mediaStreams"))
                    .Where(a => a.StreamingUrl.OriginalString.Contains(".mp4")).ToList();
            
            if (player is null)
            {
                mediaSourceFactory = new DefaultDataSourceFactory(this, Util.GetUserAgent(this, Application.PackageName));
                trackSelectorFactory = new AdaptiveTrackSelection.Factory();
                var renderersFactory = new DefaultRenderersFactory(this);
                player = ExoPlayerFactory.NewSimpleInstance(this, renderersFactory, new DefaultTrackSelector(trackSelectorFactory));
                playerView = FindViewById<PlayerView>(Resource.Id.playerView);
                playerView.Player = player;
                playerView.RequestFocus();
                playerView.SetControllerVisibilityListener(this);
                linksSpinner = FindViewById<AppCompatSpinner>(Resource.Id.linksSpinner);

                player.PlayWhenReady = true;
                playerView.ControllerShowTimeoutMs = 3000;
                
                if (mediaStreams != null && mediaStreams.Count > 0)
                {
                    var titles = new List<string>() { };
                    mediaStreams.ForEach(a => titles.Add(a.StreamingQuality + " - MP4"));
                    linksSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, titles);
                    linksSpinner.ItemSelected += LinksSpinner_ItemSelected;
                    linksSpinner.SetSelection(0);
                }
            }
            if (itemToPlay != null)
            {
                player.Prepare(CreateMediaSource(itemToPlay));
                playerView.KeepScreenOn = true;
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (e.Action == MotionEventActions.Down || e.Action == MotionEventActions.HoverEnter)
            {
                if (isSystemUIVisible)
                {
                    playerView.HideController();
                }
                else
                {
                    playerView.ShowController();
                }
                return true;
            }
            return base.OnTouchEvent(e);
        }

        private void VideoPlayer_SystemUiVisibilityChange(object sender, Android.Views.View.SystemUiVisibilityChangeEventArgs e)
        {
            isSystemUIVisible = e.Visibility == Android.Views.StatusBarVisibility.Visible ? true : false;
            if (isSystemUIVisible)
            {
                if (!playerView.IsControllerVisible) playerView.ShowController();
            }
            else
            {
                if (playerView.IsControllerVisible) playerView.HideController();
            }
        }

        private IMediaSource CreateMediaSource(StreamingUri link)
        {
            Android.Net.Uri uri = Android.Net.Uri.Parse(link.StreamingUrl.OriginalString);
            IMediaSource source = null;
            if (link.StreamingUrl.OriginalString.Contains(".mpd"))
            {
                source = new DashMediaSource.Factory(new DefaultDashChunkSource.Factory(mediaSourceFactory), mediaSourceFactory)
                        .SetManifestParser(new FilteringManifestParser(new DashManifestParser(), new List<StreamKey>()))
                        .CreateMediaSource(uri);
            }
            if (link.StreamingUrl.OriginalString.Contains(".m3u8"))
            {
                source = new HlsMediaSource.Factory(mediaSourceFactory)
                        .SetPlaylistParserFactory(new DefaultHlsPlaylistParserFactory(new List<StreamKey>()))
                        .CreateMediaSource(uri);
            }
            if (link.StreamingUrl.OriginalString.Contains(".mp4"))
            {
                source = new ExtractorMediaSource.Factory(mediaSourceFactory).CreateMediaSource(uri);
            }
            return source;
        }
        
        private void LinksSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var item = mediaStreams[e.Position];
            InitializePlayback(item);
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (player != null)
                player.PlayWhenReady = false;
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            player.PlayWhenReady = false;
            player.Release();
            playerView.KeepScreenOn = false;
            player = null;
        }

        private void HideSoftwareMenuBars()
        {
            int uiOptions = (int)Window.DecorView.SystemUiVisibility;
            uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.LayoutFullscreen | (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.LayoutHideNavigation | (int)SystemUiFlags.HideNavigation;

            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
            isSystemUIVisible = false;
        }

        private void ShowSoftwareMenuBars()
        {
            int uiOptions = (int)Window.DecorView.SystemUiVisibility;
            uiOptions |= (int)SystemUiFlags.Visible;
            uiOptions |= (int)SystemUiFlags.LayoutFullscreen;
            uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;

            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
            Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
            isSystemUIVisible = true;
        }

        public void OnVisibilityChange(int visibility)
        {
            if ((ViewStates)visibility == ViewStates.Gone || (ViewStates)visibility == ViewStates.Invisible)
            {
                HideSoftwareMenuBars();
            }
            else
            {
                ShowSoftwareMenuBars();
            }
        }
    }
}