using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using Newtonsoft.Json;

namespace com.aa.tvshows.Helper
{
    public class LoadingViewDialogForWebView : WebViewClient
    {
        public static LoadingViewDialogForWebView Instance { get; } = new LoadingViewDialogForWebView();

        AlertDialog Dialog { get; set; }
        AlertDialog.Builder Builder { get; set; }
        WebView webView;
        Button hostContinueBtn;
        string host;
        JavaValueCallback javaCallback;
        AppCompatActivity parentActivity;
        public event EventHandler OnCancelled = delegate { };

        public const int WebViewID = Resource.Id.host_view;
        public const int LoadingViewID = Resource.Id.loading_parent;
        public const int LoadingBtnID = Resource.Id.host_continue_btn;
        public const int HostPanelID = Resource.Id.host_panel;

        public LoadingViewDialogForWebView()
        {
        }

        public void ShowDialog(AppCompatActivity context, bool continueSilent = true)
        {
            if (Dialog != null && Dialog.IsShowing) return;

            parentActivity = context;
            Builder = new AlertDialog.Builder(context).SetView(LayoutInflater.From(context).Inflate(Resource.Layout.loading_webview_dialog, null, false));
            Dialog = Builder.Create();
            Dialog.SetCancelable(true);
            Dialog.SetCanceledOnTouchOutside(false);
            Dialog.DismissEvent += Dialog_DismissEvent;
            Dialog.CancelEvent += Dialog_DismissEvent;
            Dialog.Show();
            if (webView == null)
            {
                webView = Dialog.FindViewById<WebView>(WebViewID);
                webView.Settings.DomStorageEnabled = true;
                webView.Settings.JavaScriptEnabled = true;
                webView.Settings.SetPluginState(WebSettings.PluginState.On);
                webView.SetWebViewClient(this);
            }
            if (hostContinueBtn == null)
            {
                hostContinueBtn = Dialog.FindViewById<Button>(LoadingBtnID);
                hostContinueBtn.Click += delegate
                {
                    //webView.EvaluateJavascript();
                };
            }
            if (javaCallback == null)
            {
                javaCallback = new JavaValueCallback();
                javaCallback.ValueReceived += JavaCallback_ValueReceived;
            }
            if (continueSilent)
            {
                HideView(HostPanelID);
            }
            else
            {
                ShowView(HostPanelID);
            }
        }

        private void Dialog_DismissEvent(object sender, EventArgs e)
        {
            OnCancelled?.Invoke(sender, e);
            webView?.StopLoading();
            webView = null;
            hostContinueBtn = null;
        }

        public void HideDialog()
        {
            if (Dialog == null) return;

            if (Dialog.IsShowing)
            {
                Dialog.Dismiss();
            }
        }

        public void ShowView(int viewId)
        {
            Dialog.FindViewById<View>(viewId).Visibility = ViewStates.Visible;
        }

        public void HideView(int viewId)
        {
            Dialog.FindViewById<View>(viewId).Visibility = ViewStates.Gone;
        }

        public void LoadUrl(string url, string hostName = default)
        {
            webView.LoadUrl(url);
            host = hostName;
        }

        public override void OnPageStarted(WebView view, string url, Bitmap favicon)
        {
            base.OnPageStarted(view, url, favicon);
            ShowView(LoadingViewID);
        }

        public override void OnPageFinished(WebView view, string url)
        {
            base.OnPageFinished(view, url);
            if (url.Contains(WebData.BaseUrl))
            {
                view.EvaluateJavascript(WebData.GetBaseLinkScript(host), javaCallback);
            }
            else
            {

            }
            HideView(LoadingViewID);
        }

        private async void JavaCallback_ValueReceived(object sender, string e)
        {
            if (Uri.TryCreate(e, UriKind.Absolute, out Uri link))
            {
                ShowView(LoadingViewID);
                bool streamableLinkFound = false;
                if (link.Host.Contains("abcvideo") || link.Host.Contains("clipwatching") || link.Host.Contains("cloudvideo") || link.Host.Contains("gounlimited")
                    || link.Host.Contains("mixdrop") || link.Host.Contains("upstream") || link.Host.Contains("videobin") || link.Host.Contains("vidia"))
                {
                    if (await WebData.GetStreamingUrlFromDecodedLink(e) is List<StreamingUri> links)
                    {
                        foreach (var uri in links)
                        {
                            if (uri.StreamingUrl != null)
                            {
                                streamableLinkFound = true;
                                break;
                            }
                        }
                        if (streamableLinkFound)
                        {
                            HideDialog();
                            var intent = new Intent(parentActivity, typeof(PlayerActivity));
                            intent.PutExtra("mediaStreams", JsonConvert.SerializeObject(links));
                            parentActivity.StartActivity(intent);
                        }
                    }
                    if (!streamableLinkFound)
                    {
                        Error.Instance.ShowErrorTip("Error: Video not found on the given link. Please select another one.", parentActivity);
                    }
                }
                else
                {
                    // find mp4 link through captcha
                }
            }
            else
            {
                Error.Instance.ShowErrorTip("Error: Deciphering the video link failed.", parentActivity);
            }
        }
    }
}