﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

namespace com.aa.tvshows.Helper
{
    public static class WebData
    {
        public const string BaseUrl = "https://www1.watch-series.la/";
        static readonly string TVScheduleUrl = BaseUrl + "/tvschedule";
        static readonly string TVGenresUrl = BaseUrl + "/genres/";
        static readonly string TVShowDetailUrl = BaseUrl + "/serie/";
        static readonly string TVSearchUrl = BaseUrl + "/search/";
        static readonly string TVSearchSuggestionsUrl = BaseUrl + "/show/search-shows-json/";
        static readonly string TVPopularShows = BaseUrl + "/series/";
        static readonly string TVNewEpisodes = BaseUrl + "/latest/";
        static readonly string TVPopularEpisodes = BaseUrl + "/new/";

        const string ABCVideoSourcePattern = @"(http?s:.*?mp4)";
        const string ABCVideoPosterPattern = @"image:\s.*?(http?s:.*?jpg)";
        const string ClipWatchingSourcePattern = @"(http?s:.*?(mp4|m3u8))";
        const string ClipWatchingPosterPattern = @"url\=(http?s.*?.jpg)";
        const string CloudVideoSourcePattern = @"(https.*?m3u8)|(https.*?mp4)";
        const string CloudVideoPosterPattern = @"poster\(\'(https.*?jpg)";
        const string OnlyStreamSourcePattern = @"(http?s.*?mp4).*?res\:\s?([0-9]{3,4})";
        const string GoUnlimitedSourcePattern = @"src:\s?.*?(http?.*?mp4)";
        const string JetLoadSourcePattern = @"source.*(https.*)\'";
        const string StreamplaySourcePattern = @"sources.*?(http.*?mpd).*?(http.*?m3u8).*?(http.*?mp4).*?\s?poster.*?(http.*?jpg)";
        const string ProstreamSourcePattern = @"sources:\s?.*?\s?(http?.*?mp4).*?poster:.*?(http.*?jpg)";
        const string UpstreamSourcePattern = @"(http.*?mp4).*(\d{3,4}p).*\s*?image.*?(http.*?jpg)";
        const string VideobinSourcePattern = @"(http.*?m3u8)|(http.*?mp4)";
        const string VideobinPosterPattern = "(http.*?jpg)";
        const string VidiaSourcePattern = @"(https.*?(\.m3u8|\.mp4)).*?(https.*?\.jpg)";

        const int CurrentYear = 2020;
        const int MinimumYear = 1990;
        public const double CancellationTokenDelayInSeconds = 15;
        public const string NothingFoundStr = "nothing found";

        public static CookieContainer Cookies { get; private set; }
        public static HttpClientHandler GlobalClientHandler { get; private set; }
        public static HttpClient GlobalClient { get; private set; }

        public static string GetBaseLinkScript(string hostName)
        {
            return "javascript:(function() { var nodeList = document.getElementsByTagName('a'); for (var i = 0; i < nodeList.length; i++) { var hrefValue = nodeList[i].getAttribute('href'); if (hrefValue.indexOf('" + hostName.Trim() + "') > -1) { return hrefValue; } } return '"+ NothingFoundStr + "'; }) ()";
        }

        public const string GetPageHtmlScript = "javascript:(function() { var htmlStr = document.documentElement.innerHTML; return htmlStr; }) ()";

        public static async Task<string> GetHtmlStringFromUrl(Uri url, CancellationTokenSource cts = default)
        {
            if (url != null)
            {
                Cookies ??= new CookieContainer();
                GlobalClientHandler ??= new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true, CookieContainer = Cookies };
                GlobalClient ??= new HttpClient(GlobalClientHandler);
                try
                {
                    GlobalClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36 Edg/85.0.564.63");
                    GlobalClient.DefaultRequestHeaders.Add("Accept", "text/html");
                    GlobalClient.DefaultRequestHeaders.Referrer = new Uri(BaseUrl);
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
                    using HttpClientHandler handler = new HttpClientHandler() { AllowAutoRedirect = false };
                    using HttpClient client = new HttpClient(handler);
                    using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTokenDelayInSeconds));

                    var response = await client.GetAsync(url, cts.Token).ConfigureAwait(true);
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
                client.DefaultRequestHeaders.Referrer = new Uri(BaseUrl);
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

        private static string FixDuplicateYear(this string title)
        {
            var regexReplacement = Regex.Replace(title, @"( \(\d{4}\))\1+", "$1");
            return regexReplacement;
        }

        public static async Task<List<EpisodeList>> GetEpisodesForMainView(DataEnum.DataType episodesType, int page = 1)
        {
            if (page < 1) page = 1;
            string episode_link = episodesType == DataEnum.DataType.NewEpisodes ? TVNewEpisodes : TVPopularEpisodes;
            if (await GetHtmlDocumentFromUrl(new Uri(episode_link + page)).ConfigureAwait(false) is HtmlDocument doc)
            {
                if (doc.DocumentNode.Descendants("ul").Where(a => a.HasClass("pagination")).FirstOrDefault() is HtmlNode paginationNode)
                {
                    foreach (var pageNode in paginationNode.ChildNodes)
                    {
                        if (pageNode.Name == "li")
                        {
                            if (pageNode.ChildNodes.Where(a => a.Name == "a").FirstOrDefault() is HtmlNode pageLinkNode)
                            {
                                if (pageLinkNode.GetAttributeValue("href", string.Empty) == TVNewEpisodes + (page + 1))
                                {
                                    page++;
                                    break;
                                }
                            }
                        }
                    }
                }
                // generate main episodes data
                var collection = new List<EpisodeList>();
                foreach (HtmlNode listing in doc.DocumentNode.Descendants("ul").Where(a => a.HasClass("listings")))
                {
                    foreach (HtmlNode listingNode in listing.ChildNodes.Where(a => a.Name == "li"))
                    {
                        if (listingNode.Descendants("a").FirstOrDefault() is HtmlNode dataHref)
                        {
                            var listItem = new EpisodeList() { ItemType = DataEnum.DataType.NewPopularEpisodes };
                            // link
                            listItem.PageLink = dataHref.GetAttributeValue("href", string.Empty);
                            var titleArray = dataHref.GetAttributeValue("title", string.Empty).Split("-", StringSplitOptions.RemoveEmptyEntries).ToList();
                            // 0 is title, 1 is Season+Episode, 2 is Episode name
                            listItem.Title = WebUtility.HtmlDecode(titleArray?.ElementAtOrDefault(0)).FixDuplicateYear().Trim();
                            listItem.EpisodeNo = WebUtility.HtmlDecode(titleArray?.ElementAtOrDefault(1).Trim() + "--" + titleArray?.ElementAtOrDefault(2).Trim());
                            listItem.NextPageNumber = page;
                            //listItem.ImageLink = "com.aa.tvshows";
                            //listItem.ImageLinkTask = GetTVShowCoverImageUrl(listItem.PageLink);
                            // add data
                            collection.Add(listItem);
                        }
                    }
                }
                return collection;
            }
            return null;
        }

        public static async Task<string> GetTVShowCoverImageUrl(string link)
        {
            if (await GetHtmlDocumentFromUrl(new Uri(link)) is HtmlDocument doc)
            {
                if (doc.DocumentNode.Descendants("img").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "image").FirstOrDefault() is HtmlNode imgNode)
                {
                    var imageLink = imgNode.GetAttributeValue("src", string.Empty);
#if DEBUG
                    new Handler(Looper.MainLooper).Post(new Java.Lang.Runnable(delegate { if (string.IsNullOrEmpty(imageLink)) Error.Instance.ShowErrorTip($"image link from {link} is empty.", Application.Context); }));
#endif
                    return imageLink;
                }
            }

            return string.Empty;
        }

        public static async Task<List<ShowList>> GetPopularShowsForMainView(int page = 1)
        {
            if (page < 1) page = 1;
            if (await GetHtmlDocumentFromUrl(new Uri(TVPopularShows + page)).ConfigureAwait(false) is HtmlDocument doc)
            {
                if (doc.DocumentNode.Descendants("ul").Where(a => a.HasClass("pagination")).FirstOrDefault() is HtmlNode paginationNode)
                {
                    foreach(var pageNode in paginationNode.ChildNodes)
                    {
                        if (pageNode.Name == "li")
                        {
                            if (pageNode.ChildNodes.Where(a => a.Name == "a").FirstOrDefault() is HtmlNode pageLinkNode)
                            {
                                if (pageLinkNode.GetAttributeValue("href", string.Empty) == TVPopularShows + (page + 1))
                                {
                                    page++;
                                    break;
                                }
                            }
                        }
                    }
                }
                // generate main episodes data
                if (doc.DocumentNode.Descendants("ul").Where(a => a.HasClass("listings")).FirstOrDefault() is HtmlNode popularEpisodes)
                {
                    var collection = new List<ShowList>();
                    foreach (HtmlNode itemDiv in popularEpisodes.ChildNodes.Where(a => a.Name == "li" && a.HasClass("category-item")))
                    {
                        if (itemDiv.ChildNodes.Where(a => a.HasClass("title-series")).FirstOrDefault() is HtmlNode dataHref)
                        {
                            ShowList listItem = new ShowList() { ItemType = DataEnum.DataType.PopularShows };
                            // link
                            string link = dataHref.GetAttributeValue("href", null);
                            listItem.PageLink = link;
                            string title = WebUtility.HtmlDecode(dataHref.GetAttributeValue("title", "-"));
                            listItem.Title = title.FixDuplicateYear();

                            string image = itemDiv.Descendants("img").FirstOrDefault()?.GetAttributeValue("src", "tvshows.aa.com");
                            listItem.ImageLink = image;
                            string showDetail = itemDiv.Descendants("div").Where(a => a.HasClass("description")).FirstOrDefault()?.InnerText.Trim();
                            listItem.EpisodeDetail = "Description: " + WebUtility.HtmlDecode(showDetail);
                            listItem.NextPageNumber = page;
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
            if (await GetHtmlDocumentFromUrl(new Uri(TVScheduleUrl)).ConfigureAwait(false) is HtmlDocument doc)
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
                                    ItemType = DataEnum.DataType.TVSchedule
                                };

                                foreach (HtmlNode text in listingRoot.Descendants("#text").Where(a => !string.IsNullOrEmpty(a.InnerText.Trim())))
                                {
                                    if (string.IsNullOrEmpty(scheduleItem.Title))
                                    {
                                        scheduleItem.Title = WebUtility.HtmlDecode(text.InnerText.Trim()).FixDuplicateYear();
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

        public static async Task<List<GenresShow>> GetGenresLatestEpisodes(string genre)
        {
            return await GetGenresShowsAndEpisodes(genre, DataEnum.GenreDataType.LatestEpisodes);
        }

        public static async Task<List<GenresShow>> GetGenresShows(string genre, int page = 1, int year = CurrentYear)
        {
            return await GetGenresShowsAndEpisodes(genre, DataEnum.GenreDataType.Shows, page, year);
        }

        private static async Task<List<GenresShow>> GetGenresShowsAndEpisodes(string genre, DataEnum.GenreDataType genreType, int page = 1, int year = 0)
        {
            if (genreType == DataEnum.GenreDataType.LatestEpisodes || genreType == DataEnum.GenreDataType.PopularEpisodes)
            {
                // not complete yet
                string genresLink = $"{$"{TVGenresUrl}{genre.ToLower()}"}{(genreType == DataEnum.GenreDataType.LatestEpisodes ? string.Empty : "/1")}";
                if (await GetHtmlDocumentFromUrl(new Uri(genresLink)) is HtmlDocument genreDoc)
                {
                    if (genreDoc.DocumentNode.Descendants("ul").Where(a => a.HasClass("listings")).FirstOrDefault() is HtmlNode listings)
                    {
                        foreach (HtmlNode listing in listings.ChildNodes.Where(a => a.Name == "li"))
                        {
                            if (listing.Descendants("a").FirstOrDefault() is HtmlNode linkNode)
                            {
                                var link = linkNode.GetAttributeValue("href", string.Empty);
                                var dataText = linkNode.GetDirectInnerText().FixDuplicateYear();
                            }
                        }
                    }
                }
            }
            else if (genreType == DataEnum.GenreDataType.Shows)
            {
                genre = genre.Contains(" ") ? genre.Remove(genre.IndexOf(" ")) : genre;
                string genresLink = $"{TVGenresUrl}{genre.ToLower()}/{page}/{year}/0";
                if (await GetHtmlDocumentFromUrl(new Uri(genresLink)) is HtmlDocument genreDoc)
                {
                    if (genreDoc.DocumentNode.Descendants("ul").Where(a => a.HasClass("listings")).FirstOrDefault() is HtmlNode listings)
                    {
                        var items = new List<GenresShow>();
                        foreach (HtmlNode listing in listings.ChildNodes.Where(a => a.Name == "li"))
                        {
                            if (listing.Descendants("a").FirstOrDefault() is HtmlNode linkNode)
                            {
                                var link = linkNode.GetAttributeValue("href", string.Empty);
                                var title = linkNode.GetDirectInnerText();
                                var showYear = linkNode.Descendants("span").Where(a => a.HasClass("epnum")).FirstOrDefault()?.InnerText.Trim();
                                items.Add(new GenresShow()
                                {
                                    GenreType = WebUtility.HtmlDecode(genre),
                                    PageLink = link,
                                    Title = WebUtility.HtmlDecode(title).FixDuplicateYear(),
                                    ReleaseYear = WebUtility.HtmlDecode(showYear)
                                });
                            }
                        }
                        return items;
                    }
                }
            }
            return null;
        }

        public static async Task<List<SearchSuggestionsData>> GetTVShowSearchSuggestions(string query)
        {
            if (await GetJsonFromUrl(new Uri(TVSearchSuggestionsUrl + WebUtility.HtmlEncode(query))) is JArray docStr)
            {
                if (docStr.LastOrDefault()?.Value<string>("value")?.ToUpperInvariant() == "MORE RESULTS...")
                {
                    docStr.Remove(docStr.Last);
                }
                var data = new List<SearchSuggestionsData>();
                foreach (var item in docStr)
                {
                    data.Add(new SearchSuggestionsData()
                    {
                        Title = item.Value<string>("label").FixDuplicateYear(),
                        ItemLink = TVShowDetailUrl + item.Value<string>("seo_url")
                    });
                }
                return data;
            }
            return null;
        }

        public static async Task<List<SearchList>> GetTVShowSearchResults(string query, int nextPage = 1)
        {
            if (await GetHtmlDocumentFromUrl(new Uri(TVSearchUrl + WebUtility.HtmlEncode(query) + $"/page/{nextPage}/sortby/MATCH")) is HtmlDocument doc)
            {
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("search-item-left")) is IEnumerable<HtmlNode> searchItems)
                {
                    // find next page
                    if (doc.DocumentNode.Descendants("ul").Where(a => a.HasClass("pagination")).FirstOrDefault() is HtmlNode pages)
                    {
                        if (pages.Descendants("li").Where(a => a.InnerText.Trim() == "Next Page").FirstOrDefault() is HtmlNode nextPageNode)
                        {
                            if (nextPageNode.Descendants("a").FirstOrDefault() is HtmlNode pageLinkNode)
                            {
                                if (pageLinkNode.GetAttributeValue("href", string.Empty)
                                    .Equals(TVSearchUrl + WebUtility.HtmlEncode(query) + $"/page/{nextPage + 1}/sortby/MATCH"))
                                {
                                    nextPage += 1;
                                }
                                else
                                {
                                    nextPage = 0;
                                }
                            }
                        }
                        else
                        {
                            nextPage = 0;
                        }
                    }

                    var data = new List<SearchList>();
                    foreach (HtmlNode searchItem in searchItems)
                    {
                        var image = searchItem.Descendants("img").FirstOrDefault()?.GetAttributeValue("src", string.Empty);
                        if (searchItem.Descendants("div").Where(a => a.GetAttributeValue("valign", string.Empty) == "top").FirstOrDefault() is HtmlNode detailDiv)
                        {
                            var link = detailDiv.Descendants("a").FirstOrDefault()?.GetAttributeValue("href", string.Empty);
                            var title = detailDiv.Descendants("a").FirstOrDefault()?.InnerText.Trim();
                            var search = new SearchList()
                            {
                                ImageLink = image,
                                ItemType = DataEnum.DataType.Search,
                                NextPage = nextPage,
                                PageLink = link,
                                Title = WebUtility.HtmlDecode(title.FixDuplicateYear())
                            };
                            data.Add(search);
                        }
                    }
                    return data;
                }
            }
            return null;
        }

        public static async Task<SeriesDetails> GetDetailsForTVShowSeries(string link)
        {
            if (await GetHtmlDocumentFromUrl(new Uri(link)) is HtmlDocument doc)
            {
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("show-summary")).FirstOrDefault() is HtmlNode summaryDiv)
                {
                    var itemData = new SeriesDetails() { SeriesLink = link };
                    if (doc.DocumentNode.Descendants("h1").Where(a => a.HasClass("channel-title")).FirstOrDefault() is HtmlNode _titleNode)
                    {
                        if (_titleNode.Descendants("span").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "name").FirstOrDefault() is HtmlNode titleNode)
                        {
                            itemData.Title = WebUtility.HtmlDecode(titleNode.GetDirectInnerText().Trim());
                        }
                    }
                    itemData.ImageLink = summaryDiv.Descendants("img").FirstOrDefault()?.GetAttributeValue("src", string.Empty);
                    if (summaryDiv.Descendants("span").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "startDate").FirstOrDefault() is HtmlNode releaseDateNode)
                    {
                        itemData.ReleaseDate = releaseDateNode.InnerText.Trim();
                    }

                    if (summaryDiv.Descendants("strong").Where(a => a.InnerText.Trim().StartsWith("Genre:", StringComparison.InvariantCulture)).FirstOrDefault() is HtmlNode genresNode)
                    {
                        itemData.Genres = WebUtility.HtmlDecode(genresNode.InnerText.Trim());
                    }

                    if (summaryDiv.Descendants("strong").Where(a => a.GetDirectInnerText().Trim() == "Description:").FirstOrDefault() is HtmlNode descriptionNode)
                    {
                        itemData.Description = WebUtility.HtmlDecode(descriptionNode.NextSibling?.GetDirectInnerText().Trim());
                    }

                    // last episode in main doc
                    // next episode in main doc
                    if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("latest-episode")).FirstOrDefault() is HtmlNode lastEpisode)
                    {
                        foreach (HtmlNode episodeNode in lastEpisode.Descendants("a").Where(a => !string.IsNullOrEmpty(a.GetAttributeValue("href", string.Empty))))
                        {
                            // first one is last episode
                            string epAirDate = "Unknown";
                            if (episodeNode.NextSibling != null && episodeNode.NextSibling.NodeType == HtmlNodeType.Text)
                            {
                                epAirDate = episodeNode.NextSibling.InnerText.Split("\n").FirstOrDefault()?.Trim();
                            }
                            var episode = new ShowEpisodeDetails()
                            {
                                EpisodeFullNameNumber = episodeNode.GetDirectInnerText().Trim(),
                                EpisodeLink = episodeNode.GetAttributeValue("href", string.Empty),
                                EpisodeAirDate = epAirDate
                            };

                            if (itemData.LastEpisode == null)
                            {
                                itemData.LastEpisode = episode;
                            }
                            else
                            {
                                itemData.NextEpisode = episode;
                            }
                        }
                    }

                    // get seasons and episodes
                    if (doc.DocumentNode.Descendants("div").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "season") is IEnumerable<HtmlNode> seasons)
                    {
                        foreach (HtmlNode seasonNode in seasons)
                        {
                            if (itemData.Seasons == null) itemData.Seasons = new List<SeasonData>();
                            var episodesList = new List<ShowEpisodeDetails>();
                            if (seasonNode.Descendants("ul").Where(a => a.Id.StartsWith("listing", StringComparison.InvariantCulture)).FirstOrDefault() is HtmlNode episodesNode)
                            {
                                if (episodesNode.Descendants("li").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "episode") is IEnumerable<HtmlNode> episodes)
                                {
                                    foreach (HtmlNode ep in episodes)
                                    {
                                        var epTtitle = ep.Descendants("span").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "name").FirstOrDefault()?.GetDirectInnerText().Trim();
                                        var epDate = ep.Descendants("span").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "datepublished").FirstOrDefault()?.GetDirectInnerText().Trim();
                                        var epLink = ep.Descendants("meta").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "url").FirstOrDefault()?.GetAttributeValue("content", string.Empty);
                                        var epNumber = ep.Descendants("meta").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "episodenumber").FirstOrDefault()?.GetAttributeValue("content", string.Empty);
                                        var epLinks = ep.Descendants("span").Where(a => a.HasClass("epnum")).FirstOrDefault()?.InnerText.Trim();

                                        episodesList.Add(new ShowEpisodeDetails()
                                        {
                                            EpisodeShowTitle = WebUtility.HtmlDecode(epTtitle),
                                            EpisodeAirDate = epDate,
                                            EpisodeLink = epLink,
                                            EpisodeFullNameNumber = epNumber,
                                            IsEpisodeWatchable = epLinks != "(0 links)"
                                        });
                                    }
                                }
                            }

                            itemData.Seasons.Add(new SeasonData()
                            {
                                SeasonName = seasonNode.Descendants("a").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "url").FirstOrDefault()?.InnerText.Trim(),
                                Episodes = new List<ShowEpisodeDetails>(episodesList)
                            });
                        }
                    }
                    return itemData;
                }
            }
            return null;
        }

        public static async Task<ShowEpisodeDetails> GetDetailsForTVShowEpisode(string link)
        {
            if (await GetHtmlDocumentFromUrl(new Uri(link)) is HtmlDocument doc)
            {
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("fullwrap")).FirstOrDefault() is HtmlNode summaryDiv)
                {
                    var itemData = new ShowEpisodeDetails();
                    if (doc.DocumentNode.Descendants("h1").Where(a => a.HasClass("channel-title")).FirstOrDefault() is HtmlNode _titleNode)
                    {
                        if (_titleNode.Descendants("a").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "url").FirstOrDefault() is HtmlNode showUrlNode)
                        {
                            itemData.EpisodeShowLink = showUrlNode.GetAttributeValue("href", string.Empty);
                        }
                        if (_titleNode.Descendants("span").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "name").FirstOrDefault() is HtmlNode titleNode)
                        {
                            itemData.EpisodeShowTitle = WebUtility.HtmlDecode(titleNode.GetDirectInnerText().Trim());
                        }
                    }
                    itemData.EpisodeImage = summaryDiv.Descendants("img").FirstOrDefault()?.GetAttributeValue("src", string.Empty);

                    if (summaryDiv.Descendants("div").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "episode").FirstOrDefault() is HtmlNode episodeNode)
                    {
                        if (episodeNode.Descendants("meta").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "name").FirstOrDefault() is HtmlNode episodename)
                        {
                            itemData.EpisodeFullNameNumber = episodename.GetAttributeValue("content", string.Empty);
                            itemData.EpisodeNumber = episodename.GetAttributeValue("content", string.Empty);
                        }
                        if (summaryDiv.Descendants("meta").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "name").FirstOrDefault() is HtmlNode seasonName)
                        {
                            itemData.EpisodeFullNameNumber += " (" + seasonName.GetAttributeValue("content", string.Empty);
                        }
                        if (summaryDiv.Descendants("meta").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "seasonNumber").FirstOrDefault() is HtmlNode seasonNumber)
                        {
                            itemData.EpisodeNumber += string.Format(" (S{0:00}", int.Parse(seasonNumber.GetAttributeValue("content", string.Empty)));
                        }
                        if (episodeNode.Descendants("meta").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "episodeNumber").FirstOrDefault() is HtmlNode episodeCount)
                        {
                            itemData.EpisodeFullNameNumber += " - Episode " + episodeCount.GetAttributeValue("content", string.Empty) + ")";
                            itemData.EpisodeNumber += string.Format(" - E{0:00})", int.Parse(episodeCount.GetAttributeValue("content", string.Empty)));
                        }
                    }

                    if (summaryDiv.Descendants("span").Where(a => a.GetAttributeValue("itemprop", string.Empty) == "description").FirstOrDefault() is HtmlNode descriptionNode)
                    {
                        itemData.EpisodeSummary = "Summary: " + WebUtility.HtmlDecode(descriptionNode.GetDirectInnerText().Trim());
                    }
                    if (summaryDiv.Descendants("strong").Where(a => a.GetDirectInnerText().Trim() == "Aired:").FirstOrDefault() is HtmlNode releaseNode)
                    {
                        if (releaseNode.ParentNode is HtmlNode releaseDateNode)
                        {
                            itemData.EpisodeAirDate = releaseDateNode.InnerText.Trim();
                        }
                    }

                    // get links
                    if (doc.DocumentNode.Descendants("tr").Where(a => a.Attributes.Contains("id") && a.Id.StartsWith("link_")) is IEnumerable<HtmlNode> links)
                    {
                        foreach (HtmlNode linkNode in links)
                        {
                            if (itemData.EpisodeStreamLinks == null) itemData.EpisodeStreamLinks = new List<EpisodeStreamLink>();
                            var hostLink = linkNode.Descendants("a").FirstOrDefault()?.GetAttributeValue("href", string.Empty);
                            var hostName = linkNode.Descendants("span").Where(a => a.HasClass("host")).FirstOrDefault()?.InnerText.Trim();
                            var image = linkNode.Descendants("img").FirstOrDefault()?.GetAttributeValue("src", string.Empty);
                            itemData.EpisodeStreamLinks.Add(new EpisodeStreamLink()
                            {
                                HostImage = image,
                                HostName = WebUtility.HtmlDecode(hostName),
                                HostUrl = hostLink
                            });
                        }
                    }
                    if (itemData.EpisodeStreamLinks?.Count > 0)
                    {
                        itemData.EpisodeStreamLinks = new List<EpisodeStreamLink>(itemData.EpisodeStreamLinks.OrderBy(a => a.HostName));
                        itemData.IsEpisodeWatchable = true;
                    }
                    return itemData;
                }
            }
            return null;
        }

        public static async Task<List<StreamingUri>> GetStreamingUrlFromDecodedLink(string link)
        {
            var decodedLink = new Uri(link);
            List<StreamingUri> linkList; 
            linkList = decodedLink.Host switch
            {
                //"abcvideo.cc" => await GetABCVideoStreamUrl(decodedLink),
                "clipwatching.com" => await GetClipWatchingStreamUrl(decodedLink),
                "cloudvideo.tv" => await GetCloudVideoStreamUrl(decodedLink),
                "gamovideo.com" => await GetGamoVideoStreamUrl(decodedLink),
                "gounlimited.to" => await GetGoUnlimitedStreamUrl(decodedLink),
                "jetload.net" => await GetJetLoadStreamUrl(decodedLink),
                "onlystream.tv" => await GetOnlyStreamStreamUrl(decodedLink),
                //"streamplay.to" => await GetStreamplayStreamUrl(decodedLink),
                "mixdrop.co" => await GetMixdropStreamUrl(decodedLink),
                //"powvideo.net" => await GetPowvideoStreamUrl(decodedLink),    // not working yet -- requires re-captcha
                "prostream.to" => await GetProstreamStreamUrl(decodedLink),
                "upstream.to" => await GetUpstreamStreamUrl(decodedLink),
                "videobin.co" => await GetVideobinStreamUrl(decodedLink),
                "vidia.tv" => await GetVidiaStreamUrl(decodedLink),


                _ => null
            };
            return linkList;
        }

        private static async Task<List<StreamingUri>> GetABCVideoStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc == null)
            {
                decodedLink = new Uri(decodedLink.OriginalString.Insert(decodedLink.OriginalString.LastIndexOf("/") + 1, "embed-"));
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("(p,a,c,k,e,d)")).FirstOrDefault() is HtmlNode scriptNode)
            {
                var script = scriptNode.InnerText.Replace("eval", string.Empty);
                var value = new Jint.Engine().Execute(script).GetCompletionValue().ToString();

            }
            // try direct download
            if (doc.DocumentNode.Descendants("table").Where(a => a.GetAttributeValue("class", string.Empty) == "tbl1").FirstOrDefault() is HtmlNode downloadOP)
            {
                var linkList = new List<StreamingUri>();
                foreach (HtmlNode downloadLinkNode in downloadOP.Descendants("a").Where(a => a.GetAttributeValue("onclick", string.Empty).StartsWith("download_video")))
                {
                    var nodeValue = downloadLinkNode.GetAttributeValue("onclick", string.Empty).Replace("download_video", string.Empty).Replace(")", string.Empty).Replace("(", string.Empty).Replace("'", string.Empty).Split(",");
                    var nodeValueLink = "https://" + decodedLink.Host + $"/dl?op=download_orig&id={nodeValue[0]}&mode={nodeValue[1]}&hash={nodeValue[2]}";
                    var dlDoc = await GetHtmlDocumentFromUrl(new Uri(nodeValueLink));
                    if (dlDoc != null)
                    {
                        if (dlDoc.DocumentNode.Descendants("Form").Where(a => a.GetAttributeValue("method", string.Empty) == "POST").FirstOrDefault() is HtmlNode dlForm)
                        {
                            Dictionary<string, string> webCollection = GetFormInputNodes(dlForm);
                            if (await PostHtmlContentToUrl(new Uri(nodeValueLink), webCollection) is HtmlDocument postDoc)
                            {
                                if (postDoc.DocumentNode.Descendants("a").Where(a => a.GetAttributeValue("href", string.Empty).Contains(".mp4")).FirstOrDefault() is HtmlNode mp4Link)
                                {
                                    linkList.Add(new StreamingUri()
                                    {
                                        StreamingUrl = new Uri(mp4Link.GetAttributeValue("href", string.Empty)),
                                        StreamingQuality = nodeValueLink.Contains("mode=h") ? "HD" : "SD"
                                    });
                                }
                            }
                        }
                        else
                        {
                            if (dlDoc.DocumentNode.Descendants("a").Where(a => a.GetAttributeValue("href", string.Empty).Contains(".mp4")).FirstOrDefault() is HtmlNode mp4Link)
                            {
                                linkList.Add(new StreamingUri()
                                {
                                    StreamingUrl = new Uri(mp4Link.GetAttributeValue("href", string.Empty)),
                                    StreamingQuality = nodeValueLink.Contains("mode=h") ? "HD" : "SD"
                                });
                            }
                        }
                    }
                }
                return linkList;
            }
            return null;
        }

        private static async Task<List<StreamingUri>> GetClipWatchingStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc == null)
            {
                decodedLink = new Uri(decodedLink.OriginalString.Remove(decodedLink.OriginalString.LastIndexOf("/")));
                decodedLink = new Uri(decodedLink.OriginalString.Insert(decodedLink.OriginalString.LastIndexOf("/") + 1, "embed-") + ".html");
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("sources:", StringComparison.InvariantCulture)).FirstOrDefault() is HtmlNode script)
            {
                var linkList = new List<StreamingUri>();
                foreach (Match match in Regex.Matches(script.InnerText.Trim(), ClipWatchingSourcePattern))
                {
                    var uri = new StreamingUri()
                    {
                        StreamingQuality = "HD",
                        StreamingUrl = match.Groups.ElementAtOrDefault(1) != null ? new Uri(match.Groups[1].Value
                        .Replace(",", string.Empty).Replace(".urlset/master", "/index-v1-a1")) : null
                    };
                    if (Regex.Match(script.InnerText.Trim(), ClipWatchingPosterPattern) is Match posterMatch)
                    {
                        uri.PosterUrl = posterMatch.Groups[1].Value;
                    }
                    linkList.Add(uri);
                }
                return linkList;
            }

            return null;
        }

        private static async Task<List<StreamingUri>> GetCloudVideoStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc == null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            Dictionary<string, string> webCollection = null;
            string linkToPost = string.Empty;
            if (doc.DocumentNode.Descendants("Form").FirstOrDefault() is HtmlNode form)
            {
                linkToPost = form.GetAttributeValue("action", string.Empty);
                webCollection = GetFormInputNodes(form);
            }
            if (webCollection != null)
            {
                if (await PostHtmlContentToUrl(new Uri(linkToPost), webCollection) is HtmlDocument responseDoc)
                {
                    if (responseDoc.DocumentNode.Descendants("video").Where(a => a.GetAttributeValue("source", string.Empty) != string.Empty).FirstOrDefault() is HtmlNode video)
                    {
                        var videoLink = video.Descendants("source").FirstOrDefault()?.GetAttributeValue("src", string.Empty)
                            .Replace(",", string.Empty).Replace(".urlset/master", "/index-v1-a1");
                        return new List<StreamingUri>
                        {
                            new StreamingUri()
                            {
                                PosterUrl = video.GetAttributeValue("poster", string.Empty),
                                StreamingQuality = string.IsNullOrEmpty(video.Attributes[1].Value) ? "Unknown" : video.Attributes[1].Value,
                                StreamingUrl = new Uri(videoLink)
                            }
                        };
                    }
                    else
                    {
                        if (responseDoc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("eval(function(p,a,c,k,e,d)")).FirstOrDefault() is HtmlNode videoScript)
                        {
                            var scriptStr = new Jint.Engine().Execute(videoScript.InnerText.Replace("eval", string.Empty)).GetCompletionValue().ToString();
                            if (Regex.Match(scriptStr, CloudVideoSourcePattern) is Match vidUrlMatch)
                            {
                                var videoUrl = vidUrlMatch.Value.Replace(",", string.Empty).Replace(".urlset/master", "/index-v1-a1");
                                var posterUrl = Regex.Match(scriptStr, CloudVideoPosterPattern).Groups[1].Value;
                                return new List<StreamingUri>
                                {
                                    new StreamingUri()
                                    {
                                        PosterUrl = posterUrl,
                                        StreamingQuality = "HD",
                                        StreamingUrl = new Uri(videoUrl)
                                    }
                                };
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static async Task<List<StreamingUri>> GetGamoVideoStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("(p,a,c,k,e,d)")
                && a.GetAttributeValue("type", string.Empty) == "text/javascript").FirstOrDefault() is HtmlNode script)
            {
                var inner = script.InnerText.Replace("eval", string.Empty);
                var javaEvalute = new Jint.Engine().Execute(inner).GetCompletionValue();
            }
            return null;
        }

        private static async Task<List<StreamingUri>> GetGoUnlimitedStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("(p,a,c,k,e,d)")
                && a.GetAttributeValue("type", string.Empty) == "text/javascript").FirstOrDefault() is HtmlNode script)
            {
                var inner = script.InnerText.Replace("eval", string.Empty);

                var javaEvalute = new Jint.Engine().Execute(inner).GetCompletionValue().ToString();
                var match = Regex.Match(javaEvalute, GoUnlimitedSourcePattern);
                if (match.Success && match.Groups.Count > 0)
                {
                    return new List<StreamingUri>()
                    {
                        new StreamingUri()
                        {
                            // Poster not available
                            //PosterUrl = match.Groups[2].Value,
                            StreamingQuality = "HD",
                            StreamingUrl = new Uri(match.Groups[1].Value)
                        }
                    };
                }
            }
            else
            {
                // direct download
                if (doc.DocumentNode.Descendants("div").Where(a => a.Id == "dl").FirstOrDefault() is HtmlNode dlDiv)
                {
                    if (dlDiv.Descendants("a").Where(a => a.GetAttributeValue("onclick", string.Empty).StartsWith("download_video")).FirstOrDefault() is HtmlNode dlNode)
                    {
                        var dlData = dlNode.GetAttributeValue("onclick", string.Empty).Replace("download_video", string.Empty)
                            .Replace(")", string.Empty).Replace("(", string.Empty).Replace("'", string.Empty).Split(",");
                        var nodeValueLink = "https://" + decodedLink.Host + $"/dl?op=download_orig&id={dlData[0]}&mode={dlData[1]}&hash={dlData[2]}";
                        doc = await GetHtmlDocumentFromUrl(new Uri(nodeValueLink));
                        if (doc != null)
                        {
                            if (doc.DocumentNode.Descendants("a").Where(a => a.GetAttributeValue("href", string.Empty).Contains("." + decodedLink.Host))
                                .FirstOrDefault() is HtmlNode mp4Link)
                            {
                                return new List<StreamingUri>() { new StreamingUri() { StreamingUrl = new Uri(mp4Link.GetAttributeValue("href", string.Empty)), StreamingQuality = "HD" } };
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static async Task<List<StreamingUri>> GetJetLoadStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }

            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("var player")).FirstOrDefault() is HtmlNode script)
            {
                if (Regex.Match(script.InnerText, JetLoadSourcePattern) is Match vidMatch)
                {
                    return new List<StreamingUri>() { new StreamingUri() { StreamingQuality = "HD", StreamingUrl = new Uri(vidMatch.Groups[1].Value) } };
                }
            }

            return null;
        }

        private static async Task<List<StreamingUri>> GetOnlyStreamStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("var player")).FirstOrDefault() is HtmlNode script)
            {
                var matches = Regex.Match(script.InnerText.Trim(), OnlyStreamSourcePattern);
                if (matches.Success && matches.Groups.Count > 1)
                {
                    return new List<StreamingUri>()
                        {
                            new StreamingUri()
                            {
                                StreamingQuality = string.IsNullOrEmpty(matches.Groups[2].Value) ? matches.Groups[2].Value : "Unknown",
                                StreamingUrl = new Uri(matches.Groups[1].Value)
                            }
                        };
                }
            }
            return null;
        }

        /*
        private static async Task<List<StreamingUri>> GetMyStreamStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            // convert mystream.to/watch => embed.mystream.to
            // load full page and then get all string and find mp4 link
            return null;
        }
        */

        private static async Task<List<StreamingUri>> GetMixdropStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                decodedLink = new Uri(decodedLink.OriginalString.Replace("/f/", "/e/"));
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("MDCore.ref", StringComparison.InvariantCulture))
                .FirstOrDefault() is HtmlNode script)
            {
                var playerScript = new Jint.Engine().Execute(script.InnerText.Replace("eval", string.Empty).Replace("MDCore.", "var ")).GetCompletionValue().ToString();
                var scriptEntries = playerScript.Split(";", StringSplitOptions.RemoveEmptyEntries);
                if (scriptEntries.Where(s => s.Contains("jpg")).FirstOrDefault() is string poster
                    && scriptEntries.Where(s => s.Contains("mp4?")).FirstOrDefault() is string file)
                {
                    var matchPattern = ".*?(//.*)";
                    poster = Regex.Match(poster.Replace("\"", string.Empty), matchPattern).Groups[1].Value;
                    file = Regex.Match(file.Replace("\"", string.Empty), matchPattern).Groups[1].Value;
                    return new List<StreamingUri>() 
                    {
                        new StreamingUri()
                        {
                            PosterUrl = "https:" + poster,
                            StreamingQuality = "HD",
                            StreamingUrl = new Uri("https:" + file)
                        }
                    };
                }
            }

            return null;
        }

        /*
        private static async Task<List<StreamingUri>> GetPowvideoStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            // requires recaptcha
            return null;
        }
        */

        private static async Task<List<StreamingUri>> GetStreamplayStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("form").FirstOrDefault() is HtmlNode form)
            {
                var webCollection = GetFormInputNodes(form);
                var streamplayPostUrl = string.Empty;
                if (doc.DocumentNode.Descendants("script").Where(a => a.GetAttributeValue("type", string.Empty) == "text/javascript" &&
                    a.InnerText.Contains(".ready(function()")).FirstOrDefault() is HtmlNode postUrlScript)
                {
                    if (Regex.Match(postUrlScript.InnerText, @"\'action\'\,\s\'(https.*?)\'") is Match postUrlMatch)
                    {
                        if (postUrlMatch.Success && postUrlMatch.Groups.Count > 0)
                        {
                            streamplayPostUrl = postUrlMatch.Groups[1].Value + "/";
                        }
                    }
                }
                if (webCollection != null && !string.IsNullOrEmpty(streamplayPostUrl))
                {
                    if (await PostHtmlContentToUrl(new Uri(streamplayPostUrl + webCollection["id"]), webCollection) is HtmlDocument responseDoc)
                    {
                        string playerDataScript = string.Empty;
                        webCollection = GetFormInputNodes(responseDoc.DocumentNode.Descendants("form").FirstOrDefault());
                        if (webCollection == null)
                        {
                            if (responseDoc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("(p,a,c,k,e,d)")).FirstOrDefault()
                                is HtmlNode script)
                            {
                                playerDataScript = new Jint.Engine().Execute(script.InnerText.Replace("eval", string.Empty)).GetCompletionValue().ToString();
                            }
                        }
                        else
                        {
                            if (await PostHtmlContentToUrl(new Uri(streamplayPostUrl + webCollection["id"]), webCollection) is HtmlDocument responseVideoDoc)
                            {
                                if (responseVideoDoc.DocumentNode.Descendants("video").FirstOrDefault() is HtmlNode video)
                                {
                                    if (responseVideoDoc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("(p,a,c,k,e,d)")).FirstOrDefault()
                                        is HtmlNode script)
                                    {
                                        playerDataScript = new Jint.Engine().Execute(script.InnerText.Trim().Replace("eval", string.Empty)).ToString();
                                    }
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(playerDataScript))
                        {
                            var match = Regex.Match(playerDataScript, StreamplaySourcePattern);
                            if (match.Success)
                            {
                                var items = new List<StreamingUri>();
                                for (int i = 1; i < match.Groups.Count; i++)
                                {
                                    if (match.Groups[i].Value.Contains("jpg"))
                                    {
                                        break;
                                    }
                                    items.Add(new StreamingUri()
                                    {
                                        PosterUrl = match.Groups.LastOrDefault().Value,
                                        StreamingQuality = "HD",
                                        StreamingUrl = new Uri(match.Groups[i].Value)
                                    });
                                }
                                return items;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static async Task<List<StreamingUri>> GetProstreamStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("(p,a,c,k,e,d)")
                && a.GetAttributeValue("type", string.Empty) == "text/javascript").FirstOrDefault() is HtmlNode script)
            {
                var inner = script.InnerText.Replace("eval", string.Empty);
                var javaEvalute = new Jint.Engine().Execute(inner).GetCompletionValue().ToString();
                var match = Regex.Match(javaEvalute, ProstreamSourcePattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return new List<StreamingUri>()
                        {
                            new StreamingUri()
                            {
                                PosterUrl = match.Groups[2].Value,
                                StreamingQuality = "HD",
                                StreamingUrl = new Uri(match.Groups[1].Value)
                            }
                        };
                }
            }
            return null;
        }

        private static async Task<List<StreamingUri>> GetUpstreamStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("jwplayer(\"vplayer\")")
                && a.GetAttributeValue("type", string.Empty) == "text/javascript").FirstOrDefault() is HtmlNode script)
            {
                var inner = script.InnerText.Trim();
                var match = Regex.Match(inner, UpstreamSourcePattern);
                if (match.Success)
                {
                    return new List<StreamingUri>()
                        {
                            new StreamingUri()
                            {
                                PosterUrl = match.Groups[3].Value,
                                StreamingQuality = string.IsNullOrEmpty(match.Groups[2].Value) ? match.Groups[2].Value : "Unknown",
                                StreamingUrl = new Uri(match.Groups[1].Value)
                            }
                        };
                }
            }
            return null;
        }

        private static async Task<List<StreamingUri>> GetVideobinStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script").Where(a => a.InnerText.Contains("var player")
                && a.GetAttributeValue("type", string.Empty) == "text/javascript").FirstOrDefault() is HtmlNode script)
            {
                var inner = script.InnerText.Trim();
                var items = new List<StreamingUri>();
                var vMatch = Regex.Matches(inner, VideobinSourcePattern);
                var pMatch = Regex.Match(inner, VideobinPosterPattern);
                foreach (Match match in vMatch)
                {
                    if (match.Success)
                    {
                        items.Add(new StreamingUri()
                        {
                            PosterUrl = pMatch.Success ? pMatch.Value : string.Empty,
                            StreamingQuality = match.Value.Contains("m3u8") ? "HLS" : "HD",
                            StreamingUrl = new Uri(match.Value.Replace(",", string.Empty).Replace(".urlset/master", "/index-v1-a1"))
                        });
                    }
                }
                return items;
            }
            return null;
        }

        private static async Task<List<StreamingUri>> GetVidiaStreamUrl(Uri decodedLink = default, HtmlDocument doc = default)
        {
            if (doc is null)
            {
                doc = await GetHtmlDocumentFromUrl(decodedLink);
            }
            if (doc.DocumentNode.Descendants("script")
                .Where(a => a.InnerText.Contains("function(p,a,c,k,e,d)")).FirstOrDefault() is HtmlNode script)
            {
                var inner = script.InnerText.Replace("eval", string.Empty);
                var javaEvalute = new Jint.Engine().Execute(inner).GetCompletionValue().ToString();
                var match = Regex.Match(javaEvalute, VidiaSourcePattern);
                if (match.Success)
                {
                    var items = new List<StreamingUri>
                    {
                        new StreamingUri()
                        {
                            PosterUrl = match.Groups[3].Value,
                            StreamingQuality = "HD",
                            StreamingUrl = new Uri(match.Groups[1].Value)
                        }
                    };
                    return items;
                }
            }
            return null;
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
}
