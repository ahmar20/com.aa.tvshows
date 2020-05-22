using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Webkit;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using Google.Android.Material.AppBar;
using Java.Interop;
using Newtonsoft.Json;
using Square.Picasso;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Episode Detail", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class EpisodeDetailActivity : AppCompatActivity
    {
        AppBarLayout appBarLayout;
        Toolbar toolbar;
        CollapsingToolbarLayout collapseToolbar;
        RecyclerView dataRV;
        ContentLoadingProgressBar loadingView;

        Android.Widget.LinearLayout titleContainer;

        ShowEpisodeDetails epData;
        bool IsTitleContainerVisible = true;
        bool IsTitleVisible = false;
        bool canGoBackToSeriesHome = false;
        readonly int AlphaAnimationDuration = 200;
        readonly double PercentageToShowTitle = 0.600;
        readonly double PercentageToHideTitle = 0.599;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.tv_episode_detail);

            toolbar = FindViewById<Toolbar>(Resource.Id.image_toolbar_main_toolbar);
            appBarLayout = FindViewById<AppBarLayout>(Resource.Id.image_toolbar_appbar_layout);
            collapseToolbar = FindViewById<CollapsingToolbarLayout>(Resource.Id.image_toolbar_collapsing_layout);
            collapseToolbar.TitleEnabled = true;
            titleContainer = FindViewById<Android.Widget.LinearLayout>(Resource.Id.image_toolbar_collapsing_root);
            
            dataRV = FindViewById<RecyclerView>(Resource.Id.tv_episode_detail_rv);
            dataRV.SetLayoutManager(new CachingLayoutManager(this));
            loadingView = FindViewById<ContentLoadingProgressBar>(Resource.Id.image_toolbar_loading);

            AppView.SetActionBarForActivity(toolbar, this);
            appBarLayout.OffsetChanged += AppLayout_OffsetChanged;

            canGoBackToSeriesHome = Intent.GetBooleanExtra("canGoBackToSeriesHome", false);
            var link = Intent.GetStringExtra("itemLink");
            LoadEpisodeData(link);
        }

        private async void LoadEpisodeData(string link)
        {
            loadingView.Visibility = Android.Views.ViewStates.Visible;
            epData = await WebData.GetDetailsForTVShowEpisode(link);
            loadingView.Visibility = Android.Views.ViewStates.Gone;
            if (epData != null)
            {
                InvalidateOptionsMenu();
                collapseToolbar.TitleEnabled = false;
                SupportActionBar.SetDisplayShowTitleEnabled(false);

                var titleView = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_title);
                var descriptionView = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_description);
                var releaseView = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_release);
                var genreView = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_genre);
                var imageView = FindViewById<AppCompatImageView>(Resource.Id.image_toolbar_main_image);
                SupportActionBar.Title = titleView.Text = epData.EpisodeShowTitle;
                descriptionView.Text = epData.EpisodeSummary;
                releaseView.Text = epData.EpisodeAirDate;
                genreView.Text = epData.EpisodeFullNameNumber;
                SupportActionBar.Subtitle = epData.EpisodeNumber;
                Picasso.Get().Load(epData.EpisodeImage).Into(imageView);
                
                if (epData.IsEpisodeWatchable)
                {
                    var adapter = new EpisodesAdapter<EpisodeStreamLink>(DataEnum.DataType.EpisodeStreamLinks, epData.EpisodeStreamLinks);
                    dataRV.SetAdapter(adapter);
                    adapter.ItemClick += (s, e) =>
                    {
                        // handle watch episode here
                        // first get the decoded link from WebView and JavaValueCallback
                        //LoadEncodedLinkFromWebViewInBackground(adapter.GetItem(e));
                        LoadEncodedLinkFromWebView(adapter.GetItem(e));
                    };
                }
                else
                {
                    Helper.Error.Instance.ShowErrorSnack("No links found. Episode cannot be streamed.", dataRV);
                }
            }
            else
            {
                Helper.Error.Instance.ShowErrorSnack("Episode data could not be loaded.", dataRV);
            }
        }

        private void AppLayout_OffsetChanged(object sender, AppBarLayout.OffsetChangedEventArgs e)
        {
            int maxScroll = e.AppBarLayout.TotalScrollRange;
            double percentage = (double)System.Math.Abs(e.VerticalOffset) / (double)maxScroll;
            HandleAlphaOnTitle(percentage);
            HandleToolbarTitleVisibility(percentage);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (!canGoBackToSeriesHome && epData != null)
            {
                menu.Add(AppView.mainItemsGroupId, AppView.GoToSeriesHomeId, 1, "Series Home")
                    .SetIcon(Resource.Drawable.baseline_store_24)
                    .SetShowAsAction(ShowAsAction.Always);
            }   
            menu.Add(AppView.mainItemsGroupId, AppView.ReloadId, 2, "Reload")
                    .SetIcon(Resource.Drawable.baseline_refresh_24)
                    .SetShowAsAction(ShowAsAction.IfRoom);

            return AppView.ShowOptionsMenu(menu, this);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            Action seriesHome = null;
            if (item.ItemId == AppView.GoToSeriesHomeId)
            {
                seriesHome = new Action(() => 
                {
                    var intent = new Intent(this, typeof(ShowDetailActivity));
                    intent.PutExtra("itemLink", epData.EpisodeShowLink);
                    StartActivity(intent);
                });
            }
            else if (item.ItemId == AppView.ReloadId)
            {
                seriesHome = new Action(() =>
                {
                    if (loadingView.Visibility != ViewStates.Visible)
                        LoadEpisodeData(Intent.GetStringExtra("itemLink"));
                    else
                        Error.Instance.ShowErrorTip("Data is loading... Please wait!", this);
                });
            }
            return AppView.OnOptionsItemSelected(item, this, seriesHome);
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return base.OnSupportNavigateUp();
        }

        private void HandleToolbarTitleVisibility(double percentage)
        {
            if (percentage >= PercentageToShowTitle)
            {
                if (!IsTitleVisible)
                {
                    //collapseToolbar.TitleEnabled = true;
                    SupportActionBar.SetDisplayShowTitleEnabled(true);
                    //StartAlphaAnimation(toolbarText, AlphaAnimationDuration, ViewStates.Visible);
                    IsTitleVisible = true;
                }
            }
            else
            {
                if (IsTitleVisible)
                {
                    //collapseToolbar.TitleEnabled = false;
                    SupportActionBar.SetDisplayShowTitleEnabled(false);
                    //StartAlphaAnimation(toolbarText, AlphaAnimationDuration, ViewStates.Invisible);
                    IsTitleVisible = false;
                }
            }
        }

        private void HandleAlphaOnTitle(double percentage)
        {
            if (percentage >= PercentageToHideTitle)
            {
                if (IsTitleContainerVisible)
                {
                    StartAlphaAnimation(titleContainer, AlphaAnimationDuration, ViewStates.Invisible);
                    IsTitleContainerVisible = false;
                }
            }
            else
            {
                if (!IsTitleContainerVisible)
                {
                    StartAlphaAnimation(titleContainer, AlphaAnimationDuration, ViewStates.Visible);
                    IsTitleContainerVisible = true;
                }
            }
        }

        public static void StartAlphaAnimation(View v, long duration, ViewStates visibility)
        {
            AlphaAnimation alphaAnimation = visibility == ViewStates.Visible ? new AlphaAnimation(0f, 1f) : new AlphaAnimation(1f, 0f);

            alphaAnimation.Duration = duration;
            alphaAnimation.FillAfter = true;
            v.StartAnimation(alphaAnimation);
        }

        private void LoadEncodedLinkFromWebView(EpisodeStreamLink encodedLink)
        {
            LoadingViewDialogForWebView.Instance.ShowDialog(this);
            LoadingViewDialogForWebView.Instance.LoadUrl(encodedLink.HostUrl, encodedLink.HostName);
        }
    }
}