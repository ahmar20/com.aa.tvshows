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

/*
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
*/

namespace com.aa.tvshows
{
    [Activity(Label = "PlayerActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.SensorLandscape, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class PlayerActivity : AppCompatActivity
    {
        //SimpleExoPlayer player;
        //PlayerView playerView;
        //AppCompatSpinner linksSpinner;

        List<StreamingUri> mediaStreams;
        VideoView videoPlayer;
        MediaController controller;
        ContentLoadingProgressBar videoLoading;
        bool isSystemUIVisible = true;
        
        //IDataSourceFactory mediaSourceFactory;
        //TrackSelector trackSelector;
        //readonly IBandwidthMeter bandwidthMeter = new DefaultBandwidthMeter();

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
                mediaStreams = JsonConvert.DeserializeObject<List<StreamingUri>>(Intent.GetStringExtra("mediaStreams"));
            /*
            if (player is null) // if player is null
            {
                trackSelector = new DefaultTrackSelector(new AdaptiveTrackSelection.Factory());
                mediaSourceFactory = new DefaultDataSourceFactory(this, Util.GetUserAgent(this, Application.PackageName));
                player = ExoPlayerFactory.NewSimpleInstance(this, trackSelector);
                playerView = FindViewById<PlayerView>(Resource.Id.playerView);
                linksSpinner = FindViewById<AppCompatSpinner>(Resource.Id.linksSpinner);

                player.PlayWhenReady = true;
                playerView.Player = player;
                playerView.ControllerShowTimeoutMs = 3000;
                playerView.UseArtwork = true;
                
                if (mediaStreams != null)
                {
                    linksSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, mediaStreams);
                    linksSpinner.ItemSelected += LinksSpinner_ItemSelected;
                    linksSpinner.SetSelection(0);
                }
            }
            if (itemToPlay != null)
            {
                player.Prepare(CreateMediaSource(itemToPlay));
            }
            */
            if (videoPlayer is null)
            {
                videoPlayer = FindViewById<VideoView>(Resource.Id.videoPlayer);
                videoPlayer.SetZOrderOnTop(true);

                videoPlayer.Touch += VideoPlayer_Touch;
                videoPlayer.Prepared += VideoPlayer_Prepared;
                videoPlayer.Error += VideoPlayer_Error;

                controller = new Android.Widget.MediaController(this);
                videoPlayer.SetMediaController(controller);
                videoPlayer.SetLayerType(Android.Views.LayerType.Software, null);
                videoLoading = FindViewById<ContentLoadingProgressBar>(Resource.Id.videoLoading);
                if (itemToPlay is null)
                {
                    if (!(mediaStreams is null))
                    {
                        InitializePlayback(mediaStreams.Where(a => a.StreamingUrl.OriginalString.Contains(".mp4")).FirstOrDefault());
                        return;
                    }
                }
            }
            
            if (!(itemToPlay is null))
            {
                if(itemToPlay.StreamingUrl != null)
                {
                    videoLoading.Visibility = Android.Views.ViewStates.Visible;
                    videoPlayer.SetVideoURI(Android.Net.Uri.Parse(itemToPlay.StreamingUrl.OriginalString));
                }
            }
            else
            {
                Toast.MakeText(this, "No streamable link found.", ToastLength.Long).Show();
                OnBackPressed();
            }
        }

        private void VideoPlayer_Touch(object sender, Android.Views.View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Down)
            {
                if (isSystemUIVisible)
                {
                    HideSoftwareMenuBars();
                    controller.Hide();
                }
                else
                {
                    controller.Show(0);
                }
            }
        }

        private void VideoPlayer_SystemUiVisibilityChange(object sender, Android.Views.View.SystemUiVisibilityChangeEventArgs e)
        {
            isSystemUIVisible = e.Visibility == Android.Views.StatusBarVisibility.Visible ? true : false;
        }

        private void VideoPlayer_Error(object sender, Android.Media.MediaPlayer.ErrorEventArgs e)
        {
            Toast.MakeText(this, "Video error: " + e.What.ToString(), ToastLength.Long).Show();
            videoLoading.Visibility = Android.Views.ViewStates.Gone;
        }

        private void VideoPlayer_Prepared(object sender, EventArgs e)
        {
            videoLoading.Visibility = Android.Views.ViewStates.Gone;
            videoPlayer.Start();
            HideSoftwareMenuBars();
        }

        /*
        private IMediaSource CreateMediaSource(StreamingUri link)
        {
            Android.Net.Uri uri = Android.Net.Uri.Parse(link.StreamingUrl.OriginalString);
            IMediaSource source = null;
            if (link.StreamingUrl.OriginalString.Contains(".mpd"))
            {
                var dashFactory = new DefaultDashChunkSource.Factory(mediaSourceFactory);
                source = new DashMediaSource.Factory(dashFactory, mediaSourceFactory).CreateMediaSource(uri);
            }
            if (link.StreamingUrl.OriginalString.Contains(".m3u8"))
            {
                source = new HlsMediaSource.Factory(mediaSourceFactory).CreateMediaSource(uri);
            }
            if (link.StreamingUrl.OriginalString.Contains(".mp4"))
            {
                source = new ExtractorMediaSource.Factory(mediaSourceFactory).CreateMediaSource(uri);
            }
            return source;
        }
        */

        private void LinksSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var item = mediaStreams[e.Position];
            InitializePlayback(item);
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (videoPlayer != null && videoPlayer.CanPause())
                videoPlayer.Pause();
            //player.PlayWhenReady = false;
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            videoPlayer.StopPlayback();
            videoPlayer.Dispose();
            videoPlayer = null;
        }

        private void HideSoftwareMenuBars()
        {
            int uiOptions = (int)Window.DecorView.SystemUiVisibility;

            //uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.HideNavigation;
            uiOptions |= (int)SystemUiFlags.Fullscreen;

            uiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
            isSystemUIVisible = false;
        }
    }
}