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
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using Google.Android.Material.AppBar;
using Square.Picasso;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Episode Detail", ConfigurationChanges = Android.Content.PM.ConfigChanges.ScreenLayout | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class EpisodeDetailActivity : AppCompatActivity
    {
        AppBarLayout appBarLayout;
        Toolbar toolbar;
        CollapsingToolbarLayout collapseToolbar;
        RecyclerView dataRV;
        ContentLoadingProgressBar loadingView;

        Android.Widget.LinearLayout titleContainer;

        bool IsTitleContainerVisible = true;
        bool IsTitleVisible = false;
        readonly int AlphaAnimationDuration = 200;
        readonly double PercentageToShowTitle = 0.600;
        readonly double PercentageToHideTitle = 0.599;

        IMenu optionsMenu;

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
            dataRV.SetLayoutManager(new LinearLayoutManager(this));
            loadingView = FindViewById<ContentLoadingProgressBar>(Resource.Id.tv_episode_detail_loading);

            AppView.SetActionBarForActivity(toolbar, this);
            appBarLayout.OffsetChanged += AppLayout_OffsetChanged;

            var link = Intent.GetStringExtra("itemLink");
            LoadEpisodeData(link);
        }

        private async void LoadEpisodeData(string link)
        {
            loadingView.Visibility = Android.Views.ViewStates.Visible;
            ShowEpisodeDetails epData = await WebData.GetDetailsForTVShowEpisode(link);
            loadingView.Visibility = Android.Views.ViewStates.Gone;
            if (epData != null)
            {
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
                Picasso.With(this).Load(epData.EpisodeImage).Into(imageView);
                
                if (epData.IsEpisodeWatchable)
                {
                    var adapter = new EpisodesAdapter<EpisodeStreamLink>(epData.EpisodeStreamLinks, DataEnum.DataType.EpisodeStreamLinks, null);
                    dataRV.SetAdapter(adapter);
                    adapter.ItemClick += (s, e) =>
                    {
                        // handle watch episode here
                    };
                }
                else
                {
                    Error.Instance.ShowErrorSnack("No links found. Episode cannot be streamed.", dataRV);
                }
            }
            else
            {
                Error.Instance.ShowErrorSnack("Episode data could not be loaded.", dataRV);
            }
        }

        private void AppLayout_OffsetChanged(object sender, AppBarLayout.OffsetChangedEventArgs e)
        {
            int maxScroll = e.AppBarLayout.TotalScrollRange;
            double percentage = (double)Math.Abs(e.VerticalOffset) / (double)maxScroll;
            HandleAlphaOnTitle(percentage);
            HandleToolbarTitleVisibility(percentage);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            optionsMenu = menu;
            return AppView.ShowOptionsMenu(optionsMenu, this);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return AppView.OnOptionsItemSelected(item, this);
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
    }
}