﻿using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Widget;
using AndroidX.ViewPager.Widget;
using AndroidX.ViewPager2.Widget;
using com.aa.tvshows.Fragments;
using com.aa.tvshows.Helper;
using Google.Android.Material.AppBar;
using Google.Android.Material.Tabs;
using Square.Picasso;
using System;
using System.Linq;

namespace com.aa.tvshows
{
    [Activity(Label = "TV Show Detail", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ShowDetailActivity : AppCompatActivity
    {
        AppBarLayout appBarLayout;
        Toolbar detailToolbar;
        CollapsingToolbarLayout collapseToolbar;
        ViewPager2 seasonEpisodesPager;
        TabLayout seasonsHeader;

		ContentLoadingProgressBar loadingView;
		AppCompatImageView detailImage;
		AppCompatTextView titleText;
		AppCompatTextView descriptionText;
		AppCompatTextView releaseText;
		AppCompatTextView genreText;
		Android.Widget.LinearLayout titleContainer;


		SeriesDetails ShowData;
		bool IsTitleContainerVisible = true;
		bool IsTitleVisible = false;
		bool isFavorite = false;
		readonly int AlphaAnimationDuration = 200;
		readonly double PercentageToShowTitle = 0.600;
		readonly double PercentageToHideTitle = 0.599;
		readonly string ShowDetailSaveInstanceName = "showDetail";

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
            seasonEpisodesPager = FindViewById<ViewPager2>(Resource.Id.main_tabs_viewpager);
            seasonsHeader.Visibility = ViewStates.Gone;
            seasonEpisodesPager.Visibility = ViewStates.Gone;

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
			if (savedInstanceState is null)
			{
				var link = Intent.Extras.GetString("itemLink");
				LoadData(link);
			}
			else
			{
				// get saved show data
				var show = Newtonsoft.Json.JsonConvert.DeserializeObject<SeriesDetails>(savedInstanceState.GetString(ShowDetailSaveInstanceName));
				LoadData(null, show);
			}
		}

		protected override void OnSaveInstanceState(Bundle outState)
		{
			base.OnSaveInstanceState(outState);
			outState.PutString(ShowDetailSaveInstanceName, Newtonsoft.Json.JsonConvert.SerializeObject(ShowData));
		}

		private async void LoadData(string link, SeriesDetails _showData = default)
		{
			loadingView.Visibility = ViewStates.Visible;
			ShowData = _showData is null ? await WebData.GetDetailsForTVShowSeries(link) : _showData;
			loadingView.Visibility = ViewStates.Gone;
			if (ShowData != null)
			{
				if (await StorageData.IsMarkedFavorite(ShowData))
				{
					isFavorite = true;
				}
				InvalidateOptionsMenu();
				collapseToolbar.TitleEnabled = false;
				SupportActionBar.SetDisplayShowTitleEnabled(false);
				Picasso.Get().Load(ShowData.ImageLink).Into(detailImage);
				collapseToolbar.Title = titleText.Text = ShowData.Title;
				descriptionText.Text = "Description: " + ShowData.Description;
				releaseText.Text = "Released: " + ShowData.ReleaseDate;
				genreText.Text = ShowData.Genres;

                seasonsHeader.Visibility = ViewStates.Visible;
                seasonEpisodesPager.Visibility = ViewStates.Visible;
                if (ShowData.Seasons != null && ShowData.Seasons.Count > 0)
                {
                    var adapter = new PageTabsAdapter(this);
                    foreach (var season in ShowData.Seasons)
                    {
                        adapter.AddTab(new TitleFragment() { Fragmnet = new MainTabs(DataEnum.DataType.SeasonsEpisodes, season.Episodes.Cast<object>()), Title = season.SeasonName });
                    }
                    seasonsHeader.TabMode = ShowData.Seasons.Count < 5 ? TabLayout.ModeFixed : TabLayout.ModeScrollable;
                    //seasonsHeader.SetupWithViewPager(seasonEpisodesPager);
                    seasonEpisodesPager.OffscreenPageLimit = ShowData.Seasons.Count;
                    seasonEpisodesPager.Adapter = adapter;

                    TabMediatorStrategy tabStrategy = new TabMediatorStrategy(adapter.Fragments);
                    TabLayoutMediator tabMediator = new TabLayoutMediator(seasonsHeader, seasonEpisodesPager, tabStrategy);
                    tabMediator.Attach();
                }
                else
                {
                    seasonEpisodesPager.Visibility = ViewStates.Gone;
                    seasonsHeader.Visibility = ViewStates.Gone;
                    Error.Instance.ShowErrorSnack($"No episodes were found for {ShowData.Title}.", titleContainer);
                }
            }
            else
            {
                Error.Instance.ShowErrorSnack($"TV Show '{ShowData.Title}' could not be loaded.", titleContainer);
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
			if (ShowData != null)
			{
				if (isFavorite)
				{
					menu.Add(AppView.mainItemsGroupId, AppView.RemoveFavoritesId, 1, "Remove Favorite")
						.SetIcon(Resource.Drawable.baseline_favorite_24)
						.SetShowAsAction(ShowAsAction.Always);
				}
				else
				{
					menu.Add(AppView.mainItemsGroupId, AppView.AddFavoritesId, 1, "Mark Favorite")
						.SetIcon(Resource.Drawable.baseline_favorite_border_24)
						.SetShowAsAction(ShowAsAction.Always);
				}
			}
			menu.Add(AppView.mainItemsGroupId, AppView.ReloadId, 2, "Reload")
				.SetIcon(Resource.Drawable.baseline_refresh_24)
				.SetShowAsAction(ShowAsAction.IfRoom);
			return AppView.ShowOptionsMenu(menu, this);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			Action favAction = null;
			if (item.ItemId == AppView.AddFavoritesId)
			{
				favAction = new Action(async () =>
				{
					if (await StorageData.SaveSeriesToFavoritesFile(ShowData))
					{
						isFavorite = true;
						InvalidateOptionsMenu();
					}
					else
					{
						Error.Instance.ShowErrorSnack(ShowData.Title + " could not be added to favorites.", this.titleContainer);
					}
				});
			}
			else if (item.ItemId == AppView.RemoveFavoritesId)
			{
				favAction = new Action(async () =>
				{
					if (await StorageData.RemoveSeriesFromFavoritesFile(ShowData))
					{
						isFavorite = false;
						InvalidateOptionsMenu();
					}
					else
					{
						Error.Instance.ShowErrorSnack(ShowData.Title + " could not be removed from favorites.", this.titleContainer);
					}
				});
			}
			else if (item.ItemId == AppView.ReloadId)
			{
				favAction = new Action(() =>
				{
					if (loadingView.Visibility != ViewStates.Visible)
						LoadData(Intent.Extras.GetString("itemLink"));
					else
						Error.Instance.ShowErrorTip("Data is loading... Please wait!", this);
				});
			}

			return AppView.OnOptionsItemSelected(item, this, favAction);
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