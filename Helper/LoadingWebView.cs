using System;
using Android.App;
using Android.Graphics;
using Android.Webkit;

namespace com.aa.tvshows.Helper
{
    public class LoadingWebView : WebViewClient
    {
        readonly JavaValueCallback javaCallback;
        readonly string host;
        readonly Action startAction;
        readonly Action stopAction;

        public LoadingWebView(WebView webView, string host, JavaValueCallback javaCallback, Action startAction = default, Action stopAction = default)
        {
            this.host = host;
            this.javaCallback = javaCallback;
            this.startAction = startAction;
            this.stopAction = stopAction;

            if (webView is null) webView = new WebView(Application.Context);

            webView.Settings.DomStorageEnabled = true;
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.SetPluginState(WebSettings.PluginState.On);
            webView.SetWebViewClient(this);
        }

        public override void OnPageStarted(WebView view, string url, Bitmap favicon)
        {
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