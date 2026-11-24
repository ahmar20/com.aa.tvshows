using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using HtmlAgilityPack;
using Java.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace com.aa.tvshows.Helper
{
    public static class WebData
    {
        public const double CancellationTokenDelayInSeconds = 15;
        public static CookieContainer Cookies { get; private set; }
        public static HttpClientHandler GlobalClientHandler { get; private set; }
        public static HttpClient GlobalClient { get; private set; }

        public static string GetBaseLinkScript(string hostName)
        {
            return "javascript:(function() { var nodeList = document.getElementsByTagName('a'); for (var i = 0; i < nodeList.length; i++) { var hrefValue = nodeList[i].getAttribute('href'); if (hrefValue.indexOf('" + hostName.Trim() + "') > -1) { return hrefValue; } } return 'Nothing found'; }) ()";
        }

        public const string GetPageHtmlScript = "javascript:(function() { var htmlStr = document.documentElement.innerHTML; return htmlStr; }) ()";

        private static void InitializeHTTPClient()
        {
            Cookies ??= new CookieContainer();
            GlobalClientHandler ??= new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true, CookieContainer = Cookies };
            GlobalClient ??= new HttpClient(GlobalClientHandler);
        }

        public static async Task<string> GetHtmlStringFromUrl(Uri url, CancellationTokenSource cts = default)
        {
            if (url != null)
            {
                InitializeHTTPClient();
                try
                {
                    GlobalClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36 Edg/85.0.564.63");
                    GlobalClient.DefaultRequestHeaders.Add("Accept", "text/html");
                    //GlobalClient.DefaultRequestHeaders.Referrer = new Uri(BaseUrl);
                    cts ??= new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTokenDelayInSeconds));

                    var response = await GlobalClient.GetAsync(url, cts.Token).ConfigureAwait(true);
                    response.EnsureSuccessStatusCode();

                    var htmlString = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                    return htmlString;
                }
                catch (TaskCanceledException)
                {
                }
                catch (HttpRequestException)
                {
                }
                catch (Exception e)
                {
                    Error.Instance.ShowErrorTip(e.Message, Application.Context);
                }
            }
            return null;
        }

        public static async Task<HtmlDocument> GetHtmlDocumentFromUrl(Uri url, CancellationTokenSource cts = default)
        {
            if (url != null)
            {
                try
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(await GetHtmlStringFromUrl(url, cts));
                    return doc;
                }
                catch (ArgumentNullException)
                {
                }
                catch (Exception e)
                {
                    Error.Instance.ShowErrorTip(e.Message, Application.Context);
                }
            }
            return new HtmlDocument();
        }

        public static async Task<JArray> GetJsonFromUrl(Uri url)
        {
            if (url != null)
            {
                try
                {
                    InitializeHTTPClient();
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTokenDelayInSeconds));
                    var response = await GlobalClient.GetAsync(url, cts.Token).ConfigureAwait(true);
                    response.EnsureSuccessStatusCode();

                    return JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(true));
                }
                catch (Exception e)
                {
                    Error.Instance.ShowErrorTip(e.Message, Application.Context);
                }
            }
            return null;
        }

        public static async Task<HtmlDocument> PostHtmlContentToUrl(Uri url, Dictionary<string, string> content)
        {
            try
            {
                using HttpClientHandler handler = new HttpClientHandler() { AllowAutoRedirect = true };
                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html");
                //client.DefaultRequestHeaders.Referrer = new Uri(BaseUrl);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTokenDelayInSeconds));

                var response = await client.PostAsync(url, new FormUrlEncodedContent(content), cts.Token).ConfigureAwait(true);
                response.EnsureSuccessStatusCode();

                var doc = new HtmlDocument();
                doc.LoadHtml(await response.Content.ReadAsStringAsync());
                return doc;
            }
            catch (Exception e)
            {
                Error.Instance.ShowErrorTip(e.Message, Application.Context);
                return null;
            }
        }

        private static Dictionary<string, string> GetFormInputNodes(HtmlNode form)
        {
            if (form is null) return null;

            Dictionary<string, string> webCollection = null;
            foreach (var input in form.ChildNodes.Where(a => a.Name == "input" && a.GetAttributeValue("type", string.Empty) == "hidden"))
            {
                if (webCollection == null) webCollection = new Dictionary<string, string>();
                var key = input.GetAttributeValue("name", string.Empty);
                if (key.ToLowerInvariant() != "referer" && !webCollection.ContainsKey(key))
                    webCollection.Add(input.GetAttributeValue("name", string.Empty), input.GetAttributeValue("value", string.Empty));
            }
            return webCollection;
        }

        public static async Task<List<SearchHitResult>> SearchByImdbIdAsync(string tmdbId, int limit = 15, int page = 1)
        {
            InitializeHTTPClient();
            string imdbId = await TMDbClient.GetImdbId(tmdbId);
            ArgumentException.ThrowIfNullOrWhiteSpace(imdbId);
            CancellationToken cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTokenDelayInSeconds)).Token;

            // Build query parameters safely (with a dynamic analytics_tag date)
            var analyticsTag = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var queryParams = new Dictionary<string, string?>
            {
                ["q"] = imdbId,
                //["query_by"] = "post_title,category,stars,director,imdb_id",
                //["query_by_weights"] = "4,2,2,2,4",
                ["query_by"] = "imdb_id",
                ["query_by_weights"] = "4",
                ["sort_by"] = "sort_by_date:desc",
                ["limit"] = limit.ToString(),
                ["highlight_fields"] = "none",
                ["use_cache"] = "true",
                ["page"] = page.ToString(),
                ["analytics_tag"] = analyticsTag
            };

            string queryString = string.Join("&", queryParams
                .Where(kvp => kvp.Value is not null)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

            string requestUrl = $"{Sources.HDHub4U_Search_Url}?{queryString}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            request.Headers.Add("Accept-Language", "en-GB,en;q=0.9,en-US;q=0.8");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            request.Headers.Pragma.ParseAdd("no-cache");
            request.Headers.Referrer = new Uri(Sources.HDHub4U_Base_Link);
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/158.0.0.0 Safari/537.36 Edg/158.0.0.0");
            JsonDocument doc;
            try
            {
                using HttpResponseMessage response = await GlobalClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                doc = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Error.Instance.ShowErrorTip(ex.Message, Application.Context);
                return new List<SearchHitResult>();
            }


            var results = new List<SearchHitResult>();
            string pageStr = doc.RootElement.GetRawText();
            if (!doc.RootElement.TryGetProperty("hits", out JsonElement hitsElement) ||
                hitsElement.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            foreach (JsonElement hit in hitsElement.EnumerateArray())
            {
                if (!hit.TryGetProperty("document", out JsonElement document) ||
                    document.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                // Extract only the parameters you care about
                results.Add(new SearchHitResult(
                    PostTitle: GetStringOrJoined(document, "post_title"),
                    Permalink: GetStringOrJoined(document, "permalink"),
                    ImdbId: GetStringOrJoined(document, "imdb_id")
                ));
            }

            return [.. results.Where(a => a.ImdbId == imdbId)];
        }

        private static string? GetStringOrJoined(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out JsonElement prop))
                return null;

            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString(),
                JsonValueKind.Array => string.Join(", ", prop.EnumerateArray()
                    .Where(el => el.ValueKind == JsonValueKind.String)
                    .Select(el => el.GetString())),
                JsonValueKind.Number => prop.GetRawText(),
                JsonValueKind.True or JsonValueKind.False => prop.GetBoolean().ToString(),
                _ => null
            };
        }




    }

    public class JavaValueCallbackForStreamLink : Java.Lang.Object, IValueCallback
    {
        public event EventHandler<string> ValueReceived = delegate { };

        public void OnReceiveValue(Java.Lang.Object value)
        {
            var javaStr = value.ToString();
            javaStr = javaStr.Replace("\"", string.Empty);
            javaStr = WebUtility.HtmlDecode(javaStr);
            ValueReceived?.Invoke(this, javaStr);
        }
    }

    public class JavaValueCallbackForPageHtml : Java.Lang.Object, IValueCallback
    {
        public event EventHandler<string> ValueReceived = delegate { };

        public void OnReceiveValue(Java.Lang.Object value)
        {
            var javaStr = value.ToString();
            //javaStr = javaStr.Replace("\"", string.Empty);
            javaStr = WebUtility.HtmlDecode(javaStr);
            ValueReceived?.Invoke(this, javaStr);
        }
    }

    public record SearchHitResult(
            string? PostTitle,
            string? Permalink,
            string? ImdbId
        );
}
