using System;
using Android.App;
using Android.Graphics;
using Android.Webkit;

namespace com.aa.tvshows.Helper
{
    public class LoadingWebView : WebViewClient
    {
        readonly JavaValueCallbackForStreamLink javaCallback;
        readonly string host;
        readonly Action startAction;
        readonly Action stopAction;
        public bool DoNotLoadUrl { get; set; }

        public LoadingWebView(WebView webView, string host, JavaValueCallbackForStreamLink javaCallback, Action startAction = default, Action stopAction = default)
        {
            this.host = host;
            this.javaCallback = javaCallback;
            this.startAction = startAction;
            this.stopAction = stopAction;
            DoNotLoadUrl = false;

            if (webView is null) webView = new WebView(Application.Context);

            webView.Settings.DomStorageEnabled = true;
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.SetPluginState(WebSettings.PluginState.On);
            webView.SetWebViewClient(this);
        }

        public override void OnPageStarted(WebView view, string url, Bitmap favicon)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri hostLink))
            {
                if (hostLink.Host != host && hostLink.Host != "streamp1ay.cc" && !hostLink.OriginalString.Contains(WebData.BaseUrl))
                {
                    view.StopLoading();
                    return;
                }
            }
            base.OnPageStarted(view, url, favicon);
            startAction?.Invoke();
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
            stopAction?.Invoke();
        }
    }
}