using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Text;
using com.aa.tvshows.Helper;

namespace com.aa.tvshows
{
    [Activity(Label = "About")]
    public class AboutActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.about_app);
            AppView.SetActionBarForActivity(FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.main_toolbar), this);
            ShowAboutText();
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }

        private void ShowAboutText()
        {
            var devDetailsText = FindViewById<AppCompatTextView>(Resource.Id.developer_details_text);
            devDetailsText.Text =
                $"Version: {GetAppVersion()}\n" +
                "Developer: Ahmar Aftab\n" +
                "Email: ahmar20x@gmail.com\n" +
                "Github URL: https://github.com/ahmar20/com.aa.tvshows";
            var feedbackDetailsText = FindViewById<AppCompatTextView>(Resource.Id.feedback_details_text);
                feedbackDetailsText.Text =
                $"If you have any issues with the app or would like to ask for a feature, please head to {HtmlCompat.FromHtml("<a href='https://github.com/ahmar20/com.aa.tvshows/issues'>Issues</a>", HtmlCompat.FromHtmlModeLegacy)} on the project page and create an issue.\n\n" +
                $"This app uses following Open Source libraries:\n" +
                $"1. {HtmlCompat.FromHtml("<a href='https://github.com/xamarin/AndroidX'> Xamarin AndroidX </a>", HtmlCompat.FromHtmlModeLegacy)}\n" +
                $"2. {HtmlCompat.FromHtml("<a href='https://github.com/zzzprojects/html-agility-pack'> Html Agility Pack </a>", HtmlCompat.FromHtmlModeLegacy)}\n" +
                $"3. {HtmlCompat.FromHtml("<a href='https://github.com/sebastienros/jint'> Javascript Interpreter for .NET </a>", HtmlCompat.FromHtmlModeLegacy)}\n" +
                $"4. {HtmlCompat.FromHtml("<a href='https://github.com/JamesNK/Newtonsoft.Json'> Newtonsoft Json </a>", HtmlCompat.FromHtmlModeLegacy)}\n" +
                $"5. {HtmlCompat.FromHtml("<a href='https://github.com/mattleibow/square-bindings'> Square Picasso </a>", HtmlCompat.FromHtmlModeLegacy)}\n" +
                $"6. {HtmlCompat.FromHtml("<a href='https://github.com/dotnet/roslyn'> .NET Roslyn </a>", HtmlCompat.FromHtmlModeLegacy)}";
        }

        private string GetAppVersion()
        {
            try
            {
                var packageInfo = PackageManager.GetPackageInfo(PackageName, 0);
                return "v" + packageInfo.VersionName + " - " + packageInfo.VersionCode;
            }
            catch
            {
                return "NULL";
            }
        }
    }
}