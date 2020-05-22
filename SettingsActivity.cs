using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Preference;
using com.aa.tvshows.Helper;

namespace com.aa.tvshows
{
	[Activity(Label = "Settings", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class SettingsActivity : AppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your application here
			SetContentView(Resource.Layout.settings_view);
			AppView.SetActionBarForActivity(FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.main_toolbar), this);
			SupportFragmentManager.BeginTransaction().Replace(Resource.Id.settingsRoot, new SettingsViewFragment()).Commit();
		}

		public override bool OnSupportNavigateUp()
		{
			OnBackPressed();
			return true;
		}
	}

	public class SettingsViewFragment : PreferenceFragmentCompat
	{
		public SettingsViewFragment()
		{
		}

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
		{
			var rootPrefScreen = PreferenceManager.CreatePreferenceScreen(Activity);
			PreferenceScreen = rootPrefScreen;

			var uiCategory = new PreferenceCategory(Activity) { Title = "User Interface" };
			PreferenceScreen.AddPreference(uiCategory);
			uiCategory.AddPreference(new EditTextPreference(Activity)
			{
				DialogTitle = "Number of Video Links",
				DialogMessage = "Use 'all' or 0 or leave empty to show all available links or use a number to show that many. e.g. 15",
				Key = StorageData.ShowNumberOfLinksPref,
				Text = StorageData.GetNumberOfVideoLinksSetting(),
				Title = "Episode video links",
				Summary = "Choose maximum number of streaming links to display for an episode"
			});
			uiCategory.AddPreference(new CheckBoxPreference(Activity)
			{
				Key = StorageData.ShowLatestEpisodesOrderPref,
				Checked = StorageData.GetShowLatestEpisodesFirstSetting(),
				Title = "Seasons and Episodes display order",
				Summary = "Turn on to see latest episodes listed first for a TV Show",
				SummaryOff = "First episode will be listed first",
				SummaryOn = "Latest episode will be listed first"
			});

			var mediaCategory = new PreferenceCategory(Activity) { Title = "Media Player" };
			PreferenceScreen.AddPreference(mediaCategory);
			mediaCategory.AddPreference(new CheckBoxPreference(Activity)
			{
				Key = StorageData.UseExternalMediaPlayerPref,
				Title = "Choice of media player",
				Checked = StorageData.GetUseExternalMediaPlayerSetting(),
				SummaryOn = "Use external player to play video",
				SummaryOff = "Use internal player to play video"
			});
		}
	}
}