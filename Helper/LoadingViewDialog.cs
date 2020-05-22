using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.Button;

namespace com.aa.tvshows.Helper
{
    public class LoadingViewDialogForWebView
    {
        public static LoadingViewDialogForWebView Instance { get; } = new LoadingViewDialogForWebView();

        AlertDialog Dialog { get; set; }
        AlertDialog.Builder Builder { get; set; }
        
        WebView webView;
        Button hostContinueBtn;

        public const int WebViewID = Resource.Id.host_view;
        public const int LoadingViewID = Resource.Id.loading_parent;
        public const int HostContinueBtnID = Resource.Id.host_continue_btn;
        public const int HostPanelID = Resource.Id.host_panel;

        JavaValueCallback javaCallback;
        AppCompatActivity parentActivity;
        public event EventHandler OnCancelled = delegate { };

        public LoadingViewDialogForWebView()
        {
        }

        public void ShowDialog(AppCompatActivity context, bool continueSilent = true)
        {
            if (Dialog != null && Dialog.IsShowing) return;

            parentActivity = context;
            var parentView = LayoutInflater.From(context).Inflate(Resource.Layout.loading_webview_dialog, null, false);
            Builder = new AlertDialog.Builder(context).SetView(parentView);
            Dialog = Builder.Create();
            Dialog.SetCancelable(true);
            Dialog.SetCanceledOnTouchOutside(false);
            Dialog.DismissEvent += Dialog_DismissEvent;
            Dialog.CancelEvent += Dialog_DismissEvent;
            Dialog.Show();

            if (javaCallback == null)
            {
                javaCallback = new JavaValueCallback();
                javaCallback.ValueReceived += JavaCallback_ValueReceived;
            }

            if (webView == null)
            {
                webView = parentView.FindViewById<WebView>(WebViewID);
            }

            if (hostContinueBtn == null)
            {
                hostContinueBtn = parentView.FindViewById<Button>(HostContinueBtnID);
                hostContinueBtn.Click += delegate
                {
                    //webView.EvaluateJavascript();
                };
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
            new LoadingWebView(webView, host: hostName, javaCallback: javaCallback,
                    startAction: () => ShowView(LoadingViewID), stopAction: () => HideView(LoadingViewID));
            webView.LoadUrl(url);
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
                            ContextMenuDialog.Instance.ShowContextDialog(parentActivity, links);
                        }
                    }
                    if (!streamableLinkFound)
                    {
                        Error.Instance.ShowErrorTip("Error: Video not found on the given link. Please select another one.", parentActivity, ToastLength.Long);
                    }
                    HideDialog();
                }
                else
                {
                    // find mp4 link through captcha
                    Error.Instance.ShowErrorTip("Not Supported: This video hosting server is not yet supported.", parentActivity, ToastLength.Long);
                    HideDialog();
                }
            }
            else
            {
                Error.Instance.ShowErrorTip("Error: Deciphering the video link failed.", parentActivity, ToastLength.Long);
                HideDialog();
            }
        }
    }
}