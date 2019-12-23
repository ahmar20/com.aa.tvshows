﻿using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Widget;
using AndroidX.ViewPager.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Google.Android.Material.AppBar;
using Google.Android.Material.Tabs;
using Square.Picasso;
using System;
using System.Linq;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Show Detail", ConfigurationChanges = Android.Content.PM.ConfigChanges.ScreenLayout | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ShowDetailActivity : AppCompatActivity
    {
        AppBarLayout appBarLayout;
        Toolbar detailToolbar;
        CollapsingToolbarLayout collapseToolbar;
        ViewPager seasonEpisodesPager;
        TabLayout seasonsHeader;

        ContentLoadingProgressBar loadingView;
        AppCompatImageView detailImage;
        AppCompatTextView titleText;
        AppCompatTextView descriptionText;
        AppCompatTextView releaseText;
        AppCompatTextView genreText;
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
            SetContentView(Resource.Layout.tv_show_detail);

            appBarLayout = FindViewById<AppBarLayout>(Resource.Id.image_toolbar_appbar_layout);
            collapseToolbar = FindViewById<CollapsingToolbarLayout>(Resource.Id.image_toolbar_collapsing_layout);
            collapseToolbar.TitleEnabled = true;
            detailToolbar = FindViewById<Toolbar>(Resource.Id.image_toolbar_main_toolbar);

            seasonsHeader = FindViewById<TabLayout>(Resource.Id.main_tabs_header);
            seasonEpisodesPager = FindViewById<ViewPager>(Resource.Id.main_tabs_viewpager);
            seasonsHeader.TabMode = TabLayout.ModeScrollable;

            detailImage = FindViewById<AppCompatImageView>(Resource.Id.image_toolbar_main_image);
            titleText = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_title);
            descriptionText = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_description);
            releaseText = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_release);
            genreText = FindViewById<AppCompatTextView>(Resource.Id.image_toolbar_main_genre);

            titleContainer = FindViewById<Android.Widget.LinearLayout>(Resource.Id.image_toolbar_collapsing_root);
            loadingView = FindViewById<ContentLoadingProgressBar>(Resource.Id.image_toolbar_loading);

            AppView.SetActionBarForActivity(detailToolbar, this);
            appBarLayout.OffsetChanged += AppLayout_OffsetChanged;

            // load data from link
            // set title, set image
            var link = Intent.Extras.GetString("itemLink");
            LoadData(link);
        }

        private async void LoadData(string link)
        {
            loadingView.Visibility = ViewStates.Visible;
            var showData = await WebData.GetDetailsForTVShowSeries(link);
            loadingView.Visibility = ViewStates.Gone;
            if (showData != null)
            {
                collapseToolbar.TitleEnabled = false;
                SupportActionBar.SetDisplayShowTitleEnabled(false);

                Picasso.With(this).Load(showData.ImageLink).Into(detailImage);
                collapseToolbar.Title = titleText.Text = showData.Title;
                descriptionText.Text = "Description: " + showData.Description;
                releaseText.Text = "Released: " + showData.ReleaseDate;
                genreText.Text = showData.Genres;
                
                if (showData.Seasons != null && showData.Seasons.Count > 0)
                {
                    var adapter = new PageTabsAdapter(SupportFragmentManager);
                    foreach(var season in showData.Seasons)
                    {
                        adapter.AddTab(new TitleFragment() { Fragmnet = new MainTabs(DataEnum.DataType.SeasonsEpisodes, season.Episodes.Cast<object>()), Title = season.SeasonName });
                    }
                    seasonsHeader.SetupWithViewPager(seasonEpisodesPager);
                    seasonEpisodesPager.Adapter = adapter;
                }
                else
                {
                    seasonEpisodesPager.Visibility = ViewStates.Gone;
                    seasonsHeader.Visibility = ViewStates.Gone;
                    Error.Instance.ShowErrorSnack($"No episodes were found for {showData.Title}.", titleContainer);
                }
            }
            else
            {
                Error.Instance.ShowErrorSnack($"TV Show '{showData.Title}' could not be loaded.", titleContainer);
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
                    collapseToolbar.TitleEnabled = true;
                    //StartAlphaAnimation(toolbarText, AlphaAnimationDuration, ViewStates.Visible);
                    IsTitleVisible = true;
                }
            }
            else
            {
                if (IsTitleVisible)
                {
                    collapseToolbar.TitleEnabled = false;
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