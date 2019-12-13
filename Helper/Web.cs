using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HtmlAgilityPack;

namespace com.aa.tvshows.Helper
{
    public static class Web
    {
        const string BaseUrl = "https://www1.swatchseries.to";
        static string TVScheduleUrl = BaseUrl + "/tvschedule";

        public const double CancellationTokenDelayInSeconds = 40;

        public static async Task<HtmlDocument> GetHtmlFromUrl(Uri url)
        {
            if (url != null)
            {
                try
                {
                    using HttpClientHandler handler = new HttpClientHandler() { AllowAutoRedirect = false };
                    using HttpClient client = new HttpClient(handler);
                    using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTokenDelayInSeconds));

                    var response = await client.GetAsync(url, cts.Token).ConfigureAwait(true);
                    response.EnsureSuccessStatusCode();

                    var htmlString = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(htmlString);
                    return doc;
                }
                catch (Exception e)
                {
                    //Error.ErrorInstance.ShowErrorSnack(e.Message);
                }
            }
            return null;
        }

        public static async Task<List<EpisodeList>> GetPopularEpisodesForMainView()
        {
            if (await GetHtmlFromUrl(new Uri(BaseUrl)).ConfigureAwait(false) is HtmlDocument doc)
            {
                // generate main episodes data
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("block-left-home")).FirstOrDefault() is HtmlNode popularEpisodes)
                {
                    var collection = new List<EpisodeList>();
                    foreach (HtmlNode itemDiv in popularEpisodes.ChildNodes.Where(a => a.Name == "div" && a.HasClass("block-left-home-inside")))
                    {
                        if (itemDiv.ChildNodes.Where(a => a.HasClass("block-left-home-inside-text")).FirstOrDefault() is HtmlNode dataDiv)
                        {
                            var listItem = new EpisodeList() { ItemType = DataEnum.MainTabsType.NewPopularEpisodes };
                            // link
                            if (dataDiv.Descendants("a").ElementAtOrDefault(0) is HtmlNode aNode)
                            {
                                var link = aNode.GetAttributeValue("href", null);
                                listItem.PageLink = link;
                                var title = aNode.ChildNodes.Where(a => a.Name == "b").ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.Title = WebUtility.HtmlDecode(title);
                            }
                            if (dataDiv.Descendants("a").ElementAtOrDefault(1) is HtmlNode eNode)
                            {
                                var episodeDetail = eNode.ChildNodes.Where(a => a.Name == "span" && a.HasClass("p11")).ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.EpisodeNo = WebUtility.HtmlDecode(episodeDetail);
                            }
                            var image = itemDiv.Descendants("img").ElementAtOrDefault(0)?.GetAttributeValue("src", "tvshows.aa.com");
                            listItem.ImageLink = image;
                            // add data
                            collection.Add(listItem);
                        }
                    }
                    return collection;
                }
            }
            else
            {
                //Error.ErrorInstance.ShowErrorTip("");
            }
            return null;
        }

        public static async Task<List<EpisodeList>> GetNewestEpisodesForMainView()
        {
            if (await GetHtmlFromUrl(new Uri(BaseUrl)).ConfigureAwait(false) is HtmlDocument doc)
            {
                // generate main episodes data
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("block-left-home")).ElementAtOrDefault(1) is HtmlNode popularEpisodes)
                {
                    var collection = new List<EpisodeList>();
                    foreach (HtmlNode itemDiv in popularEpisodes.ChildNodes.Where(a => a.Name == "div" && a.HasClass("block-left-home-inside")))
                    {
                        if (itemDiv.ChildNodes.Where(a => a.HasClass("block-left-home-inside-text")).FirstOrDefault() is HtmlNode dataDiv)
                        {
                            var listItem = new EpisodeList() { ItemType = DataEnum.MainTabsType.NewPopularEpisodes };
                            // link
                            if (dataDiv.Descendants("a").ElementAtOrDefault(0) is HtmlNode aNode)
                            {
                                var link = aNode.GetAttributeValue("href", null);
                                listItem.PageLink = link;
                                var title = aNode.ChildNodes.Where(a => a.Name == "b").ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.Title = WebUtility.HtmlDecode(title);
                            }
                            if (dataDiv.Descendants("a").ElementAtOrDefault(1) is HtmlNode eNode)
                            {
                                var episodeDetail = eNode.ChildNodes.Where(a => a.Name == "span" && a.HasClass("p11")).ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.EpisodeNo = WebUtility.HtmlDecode(episodeDetail);
                            }
                            var image = itemDiv.Descendants("img").ElementAtOrDefault(0)?.GetAttributeValue("src", "tvshows.aa.com");
                            listItem.ImageLink = image;
                            // add data
                            collection.Add(listItem);
                        }
                    }
                    return collection;
                }
            }
            else
            {
                //Error.ErrorInstance.ShowErrorTip("");
            }
            return null;
        }

        public static async Task<List<ShowList>> GetPopularShowsForMainView()
        {
            if (await GetHtmlFromUrl(new Uri(BaseUrl)).ConfigureAwait(false) is HtmlDocument doc)
            {
                // generate main episodes data
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("block-right-home")).FirstOrDefault() is HtmlNode popularEpisodes)
                {
                    var collection = new List<ShowList>();
                    foreach (HtmlNode itemDiv in popularEpisodes.ChildNodes.Where(a => a.Name == "div" && a.HasClass("block-right-home-inside")))
                    {
                        if (itemDiv.ChildNodes.Where(a => a.HasClass("block-right-home-inside-text")).FirstOrDefault() is HtmlNode dataDiv)
                        {
                            var listItem = new ShowList() { ItemType = DataEnum.MainTabsType.PopularShows };
                            // link
                            if (dataDiv.Descendants("a").ElementAtOrDefault(0) is HtmlNode aNode)
                            {
                                var link = aNode.GetAttributeValue("href", null);
                                listItem.PageLink = link;
                                var title = aNode.Descendants("b").ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.Title = WebUtility.HtmlDecode(title);
                            }
                            var episodeDetail = dataDiv.Descendants("span").Where(a => a.HasClass("p11")).FirstOrDefault()?.InnerText?.Trim();
                            var image = itemDiv.Descendants("img").FirstOrDefault()?.GetAttributeValue("src", "tvshows.aa.com");
                            var showDetail = dataDiv.ChildNodes.Where(a => a.Name == "#text" && !string.IsNullOrEmpty(a.InnerText.Trim())).FirstOrDefault()?.InnerText.Trim();

                            listItem.EpisodeNo = WebUtility.HtmlDecode(episodeDetail);
                            listItem.ImageLink = image;
                            listItem.EpisodeDetail = "Description: " + WebUtility.HtmlDecode(showDetail);
                            // add data
                            collection.Add(listItem);
                        }
                    }
                    return collection;
                }
            }
            else
            {
                //Error.ErrorInstance.ShowErrorTip("");
            }
            return null;
        }

        public static async Task<Dictionary<string, List<CalenderScheduleList>>> GetTVSchedule()
        {
            if (await GetHtmlFromUrl(new Uri(TVScheduleUrl)).ConfigureAwait(false) is HtmlDocument doc)
            {
                if (doc.DocumentNode.Descendants("div").Where(a => a.Id == "scheduleCalendar").ElementAtOrDefault(0) is HtmlNode scheduleDiv)
                {
                    // dates
                    var dates = new Dictionary<string, List<CalenderScheduleList>>();
                    foreach (HtmlNode tabDiv in scheduleDiv.Descendants("div").Where(a => a.HasClass("slide")))
                    {
                        // dates
                        string date = null;
                        if (tabDiv.Descendants("ul").Where(a => a.HasClass("tabs")).FirstOrDefault() is HtmlNode tab)
                        {
                            if (tab.Descendants("a").Where(a => a.Id.StartsWith("calendarTab", StringComparison.InvariantCulture)).FirstOrDefault() is HtmlNode tabRoot)
                            {
                                if (tabRoot.Descendants("div").FirstOrDefault() is HtmlNode dateDiv)
                                {
                                    date = WebUtility.HtmlDecode(dateDiv.InnerText.Trim());
                                }
                            }
                        }
                        // content for each date
                        var list = new List<CalenderScheduleList>();
                        foreach (HtmlNode listing in tabDiv.Descendants("ul").Where(a => a.HasClass("listings")).FirstOrDefault()?.Descendants("li"))
                        {
                            if (listing.Descendants("a").Where(a => a.GetAttributeValue("target", string.Empty) == "_blank").FirstOrDefault() is HtmlNode listingRoot)
                            {
                                var scheduleItem = new CalenderScheduleList
                                {
                                    ImageLink = listing.Descendants("img").FirstOrDefault()?.GetAttributeValue("src", "tvshows.aa.com"),
                                    PageLink = listingRoot.GetAttributeValue("href", "tvshows.aa.com"),
                                    ItemType = DataEnum.MainTabsType.TVSchedule
                                };

                                foreach (HtmlNode text in listingRoot.Descendants("#text").Where(a => !string.IsNullOrEmpty(a.InnerText.Trim())))
                                {
                                    if (string.IsNullOrEmpty(scheduleItem.Title))
                                    {
                                        scheduleItem.Title = WebUtility.HtmlDecode(text.InnerText.Trim());
                                    }
                                    else if (string.IsNullOrEmpty(scheduleItem.EpisodeNo))
                                    {
                                        scheduleItem.EpisodeNo = WebUtility.HtmlDecode(text.InnerText.Trim());
                                    }
                                    else if (string.IsNullOrEmpty(scheduleItem.EpisodeName))
                                    {
                                        scheduleItem.EpisodeName = WebUtility.HtmlDecode(text.InnerText.Trim());
                                    }
                                }
                                list.Add(scheduleItem);
                            }
                        }
                        dates.Add(date, list);
                    }
                    return dates;
                }
            }
            else
            {

            }
            return null;
        }
    }
}