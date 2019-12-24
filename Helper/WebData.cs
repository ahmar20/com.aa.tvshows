using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace com.aa.tvshows.Helper
{
    public static class WebData
    {
        const string BaseUrl = "https://www1.swatchseries.to";
        static readonly string TVScheduleUrl = BaseUrl + "/tvschedule";
        static readonly string TVGenresUrl = BaseUrl + "/genres/";
        static readonly string TVShowDetailUrl = BaseUrl + "/serie/";
        static readonly string TVSearchUrl = BaseUrl + "/search/";
        static readonly string TVSearchSuggestionsUrl = BaseUrl + "/show/search-shows-json/";
        const int CurrentYear = 2019;
        const int MinimumYear = 1990;

        public const double CancellationTokenDelayInSeconds = 30;

        public static async Task<HtmlDocument> GetHtmlDocumentFromUrl(Uri url, CancellationTokenSource cts = default)
        {
            if (url != null)
            {
                try
                {
                    using HttpClientHandler handler = new HttpClientHandler() { AllowAutoRedirect = true };
                    using HttpClient client = new HttpClient(handler);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "text/html");
                    //client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                    client.DefaultRequestHeaders.Referrer = new Uri(BaseUrl);
                    cts ??= new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTokenDelayInSeconds));

                    var response = await client.GetAsync(url, cts.Token).ConfigureAwait(true);
                    response.EnsureSuccessStatusCode();

                    var htmlString = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(htmlString);
                    return doc;
                }
                catch (Exception e)
                {
                    //Error.Instance.ShowErrorTip(e.Message);
                }
            }
            return null;
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
                    //Error.ErrorInstance.ShowErrorSnack(e.Message);
                }
            }
            return null;
        }

        private static string FixDuplicateYear(this string title)
        {
            var regexReplacement = Regex.Replace(title, @"( \(\d{4}\))\1+", "$1");
            return regexReplacement;
        }

        public static async Task<List<EpisodeList>> GetPopularEpisodesForMainView()
        {
            if (await GetHtmlDocumentFromUrl(new Uri(BaseUrl)).ConfigureAwait(false) is HtmlDocument doc)
            {
                // generate main episodes data
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("block-left-home")).FirstOrDefault() is HtmlNode popularEpisodes)
                {
                    var collection = new List<EpisodeList>();
                    foreach (HtmlNode itemDiv in popularEpisodes.ChildNodes.Where(a => a.Name == "div" && a.HasClass("block-left-home-inside")))
                    {
                        if (itemDiv.ChildNodes.Where(a => a.HasClass("block-left-home-inside-text")).FirstOrDefault() is HtmlNode dataDiv)
                        {
                            var listItem = new EpisodeList() { ItemType = DataEnum.DataType.NewPopularEpisodes };
                            // link
                            if (dataDiv.Descendants("a").ElementAtOrDefault(0) is HtmlNode aNode)
                            {
                                var link = aNode.GetAttributeValue("href", null);
                                listItem.PageLink = link;
                                var title = aNode.ChildNodes.Where(a => a.Name == "b").ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.Title = WebUtility.HtmlDecode(title).FixDuplicateYear();
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
            if (await GetHtmlDocumentFromUrl(new Uri(BaseUrl)).ConfigureAwait(false) is HtmlDocument doc)
            {
                // generate main episodes data
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("block-left-home")).ElementAtOrDefault(1) is HtmlNode popularEpisodes)
                {
                    var collection = new List<EpisodeList>();
                    foreach (HtmlNode itemDiv in popularEpisodes.ChildNodes.Where(a => a.Name == "div" && a.HasClass("block-left-home-inside")))
                    {
                        if (itemDiv.ChildNodes.Where(a => a.HasClass("block-left-home-inside-text")).FirstOrDefault() is HtmlNode dataDiv)
                        {
                            var listItem = new EpisodeList() { ItemType = DataEnum.DataType.NewPopularEpisodes };
                            // link
                            if (dataDiv.Descendants("a").ElementAtOrDefault(0) is HtmlNode aNode)
                            {
                                var link = aNode.GetAttributeValue("href", null);
                                listItem.PageLink = link;
                                var title = aNode.ChildNodes.Where(a => a.Name == "b").ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.Title = WebUtility.HtmlDecode(title).FixDuplicateYear();
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
            if (await GetHtmlDocumentFromUrl(new Uri(BaseUrl)).ConfigureAwait(false) is HtmlDocument doc)
            {
                // generate main episodes data
                if (doc.DocumentNode.Descendants("div").Where(a => a.HasClass("block-right-home")).FirstOrDefault() is HtmlNode popularEpisodes)
                {
                    var collection = new List<ShowList>();
                    foreach (HtmlNode itemDiv in popularEpisodes.ChildNodes.Where(a => a.Name == "div" && a.HasClass("block-right-home-inside")))
                    {
                        if (itemDiv.ChildNodes.Where(a => a.HasClass("block-right-home-inside-text")).FirstOrDefault() is HtmlNode dataDiv)
                        {
                            var listItem = new ShowList() { ItemType = DataEnum.DataType.PopularShows };
                            // link
                            if (dataDiv.Descendants("a").ElementAtOrDefault(0) is HtmlNode aNode)
                            {
                                var link = aNode.GetAttributeValue("href", null);
                                listItem.PageLink = link;
                                var title = aNode.Descendants("b").ElementAtOrDefault(0)?.InnerText?.Trim();
                                listItem.Title = WebUtility.HtmlDecode(title).FixDuplicateYear();
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
                foreach(var item in docStr)
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
                    var itemData = new SeriesDetails();
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
                            if (itemData.LastEpisode == null)
                            {
                                itemData.LastEpisode = new ShowEpisodeDetails()
                                {
                                    EpisodeShowTitle = episodeNode.GetDirectInnerText().Trim(),
                                    EpisodeLink = episodeNode.GetAttributeValue("href", string.Empty)
                                };
                            }
                            else
                            {
                                itemData.NextEpisode = new ShowEpisodeDetails()
                                {
                                    EpisodeShowTitle = episodeNode.GetDirectInnerText().Trim(),
                                    EpisodeLink = episodeNode.GetAttributeValue("href", string.Empty)
                                };
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
                                            IsEpisodeWatchable = epLinks == "(0 links)" ? false : true
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
                        foreach(HtmlNode linkNode in links)
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
                        itemData.IsEpisodeWatchable = true;
                    }
                    return itemData;
                }
            }
            return null;
        }
    }
}