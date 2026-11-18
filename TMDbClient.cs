using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Newtonsoft.Json.Linq;

namespace com.aa.tvshows.Helper
{
    public static class TMDbClient
    {
        /// <summary>
        /// Set this to your TMDB API Read Access Token (v4 auth -- the long token, sent as a
        /// Bearer header, not the short v3 "API Key") before using any of these methods.
        /// Get one at https://www.themoviedb.org/settings/api
        /// </summary>
        public static string ApiKey { get; set; } = string.Empty;

        const string ApiBase = "https://api.themoviedb.org/3";
        const string ImageBase = "https://image.tmdb.org/t/p/";
        const string PosterSize = "w500";
        const string StillSize = "w780";
        const double RequestTimeoutSeconds = 15;

        public const string LinkScheme = "tmdb://tv/";

        static HttpClient _client;
        static HttpClient Client => _client ??= new HttpClient();

        static Dictionary<int, string> _genreCache;

        private static async Task<JObject> GetJsonAsync(string path, Dictionary<string, string> query = null)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                Error.Instance.ShowErrorTip("Set TMDbClient.ApiKey with your TMDB API key before browsing shows.", Application.Context);
                return null;
            }
            try
            {
                var qs = "language=en-US";
                if (query != null)
                {
                    foreach (var kv in query)
                    {
                        qs += $"&{kv.Key}={Uri.EscapeDataString(kv.Value)}";
                    }
                }
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds));
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}{path}?{qs}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                var response = await Client.SendAsync(request, cts.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    Error.Instance.ShowErrorTip($"TMDB request failed ({(int)response.StatusCode}).", Application.Context);
                    return null;
                }
                var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JObject.Parse(str);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception e)
            {
                Error.Instance.ShowErrorTip(e.Message, Application.Context);
                return null;
            }
        }

        static string Image(string path, string size) => string.IsNullOrEmpty(path) ? null : ImageBase + size + path;

        static string YearOf(string date) => string.IsNullOrEmpty(date) || date.Length < 4 ? string.Empty : date.Substring(0, 4);

        static string ShowLink(int id) => $"{LinkScheme}{id}";

        static string EpisodeLink(int id, int season, int episode) => $"{LinkScheme}{id}/season/{season}/episode/{episode}";

        public static bool TryParseShowLink(string link, out int id)
        {
            id = 0;
            if (string.IsNullOrEmpty(link) || !link.StartsWith(LinkScheme, StringComparison.OrdinalIgnoreCase)) return false;
            var rest = link.Substring(LinkScheme.Length);
            var slash = rest.IndexOf('/');
            var idPart = slash < 0 ? rest : rest.Substring(0, slash);
            return int.TryParse(idPart, out id);
        }

        public static bool TryParseEpisodeLink(string link, out int id, out int season, out int episode)
        {
            id = season = episode = 0;
            if (!IsEpisodeLink(link)) return false;
            var parts = link.Substring(LinkScheme.Length).Split('/');
            if (parts.Length != 5) return false;
            return int.TryParse(parts[0], out id) && parts[1] == "season" && int.TryParse(parts[2], out season) &&
                   parts[3] == "episode" && int.TryParse(parts[4], out episode);
        }

        /// <summary>Used by AppView.HandleItemShowEpisodeClick to decide whether a link should
        /// open the show detail screen or the episode detail screen.</summary>
        public static bool IsEpisodeLink(string link) =>
            !string.IsNullOrEmpty(link) && link.StartsWith(LinkScheme, StringComparison.OrdinalIgnoreCase) &&
            link.Contains("/season/") && link.Contains("/episode/");

        // ---------------- show listings ----------------

        private static ShowList MapShow(JToken s, int nextPage)
        {
            var id = s.Value<int>("id");
            return new ShowList
            {
                ItemType = DataEnum.DataType.PopularShows,
                PageLink = ShowLink(id),
                Title = s.Value<string>("name"),
                ImageLink = Image(s.Value<string>("poster_path"), PosterSize),
                EpisodeDetail = "Description: " + (s.Value<string>("overview") ?? string.Empty),
                NextPageNumber = nextPage
            };
        }

        public static async Task<List<ShowList>> GetPopularShowsForMainView(int page = 1)
        {
            if (page < 1) page = 1;
            var json = await GetJsonAsync("/tv/popular", new Dictionary<string, string> { ["page"] = page.ToString() });
            if (json?["results"] == null) return null;
            var totalPages = json.Value<int>("total_pages");
            var nextPage = page < totalPages ? page + 1 : page;
            return json["results"].Select(s => MapShow(s, nextPage)).ToList();
        }

        // ---------------- "new episodes" (today's airing episodes) ----------------

        public static async Task<List<EpisodeList>> GetEpisodesForMainView(DataEnum.DataType episodesType, int page = 1)
        {
            if (page < 1) page = 1;
            var json = await GetJsonAsync("/tv/airing_today", new Dictionary<string, string> { ["page"] = page.ToString() });
            if (json?["results"] == null) return null;
            var totalPages = json.Value<int>("total_pages");
            var nextPage = page < totalPages ? page + 1 : page;

            var collection = new List<EpisodeList>();
            foreach (var s in json["results"])
            {
                var id = s.Value<int>("id");
                // the list endpoint only returns show-level info, so pull the show's
                // last-aired episode to get a season/episode number to link to
                var details = await GetJsonAsync($"/tv/{id}");
                var last = details?["last_episode_to_air"];
                if (last == null) continue;

                var season = last.Value<int>("season_number");
                var episode = last.Value<int>("episode_number");
                collection.Add(new EpisodeList
                {
                    ItemType = episodesType,
                    PageLink = EpisodeLink(id, season, episode),
                    Title = s.Value<string>("name"),
                    EpisodeNo = $"S{season:00}E{episode:00} -- {last.Value<string>("name")}",
                    NextPageNumber = nextPage
                });
            }
            return collection;
        }

        // ---------------- genres ----------------

        private static async Task<Dictionary<int, string>> GetGenreMap()
        {
            if (_genreCache != null) return _genreCache;
            var json = await GetJsonAsync("/genre/tv/list");
            if (json?["genres"] == null) return new Dictionary<int, string>();
            _genreCache = json["genres"].ToDictionary(g => g.Value<int>("id"), g => g.Value<string>("name"));
            return _genreCache;
        }

        /// <summary>Replaces the hardcoded genre list in GenresActivity with TMDB's own TV genre list.</summary>
        public static async Task<List<string>> GetGenreNames()
        {
            var map = await GetGenreMap();
            return map.Values.OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static async Task<List<GenresShow>> GetGenresShows(string genre, int page = 1, int year = 0)
        {
            if (page < 1) page = 1;
            var map = await GetGenreMap();
            var match = map.FirstOrDefault(kv => string.Equals(kv.Value, genre, StringComparison.OrdinalIgnoreCase));
            if (match.Value == null) return null;

            var query = new Dictionary<string, string>
            {
                ["with_genres"] = match.Key.ToString(),
                ["page"] = page.ToString(),
                ["sort_by"] = "popularity.desc"
            };
            if (year > 0) query["first_air_date_year"] = year.ToString();

            var json = await GetJsonAsync("/discover/tv", query);
            if (json?["results"] == null) return null;

            return json["results"].Select(s => new GenresShow
            {
                GenreType = genre,
                PageLink = ShowLink(s.Value<int>("id")),
                Title = s.Value<string>("name"),
                ReleaseYear = YearOf(s.Value<string>("first_air_date"))
            }).ToList();
        }

        // ---------------- search ----------------

        public static async Task<List<SearchSuggestionsData>> GetTVShowSearchSuggestions(string query)
        {
            var json = await GetJsonAsync("/search/tv", new Dictionary<string, string> { ["query"] = query, ["page"] = "1" });
            if (json?["results"] == null) return null;

            return json["results"].Take(8).Select(s =>
            {
                var year = YearOf(s.Value<string>("first_air_date"));
                return new SearchSuggestionsData
                {
                    Title = s.Value<string>("name") + (year.Length > 0 ? $" ({year})" : string.Empty),
                    ItemLink = ShowLink(s.Value<int>("id"))
                };
            }).ToList();
        }

        public static async Task<List<SearchList>> GetTVShowSearchResults(string query, int nextPage = 1)
        {
            if (nextPage < 1) nextPage = 1;
            var json = await GetJsonAsync("/search/tv", new Dictionary<string, string> { ["query"] = query, ["page"] = nextPage.ToString() });
            if (json?["results"] == null) return null;

            var totalPages = json.Value<int>("total_pages");
            // 0 signals "no more pages" to the endless-scroll callers, matching the old convention
            var followingPage = nextPage < totalPages ? nextPage + 1 : 0;

            return json["results"].Select(s => new SearchList
            {
                ItemType = DataEnum.DataType.Search,
                PageLink = ShowLink(s.Value<int>("id")),
                Title = s.Value<string>("name"),
                ImageLink = Image(s.Value<string>("poster_path"), PosterSize),
                EpisodeDetail = "Description: " + (s.Value<string>("overview") ?? string.Empty),
                EpisodeNo = YearOf(s.Value<string>("first_air_date")),
                NextPage = followingPage
            }).ToList();
        }

        // ---------------- show & episode detail ----------------

        private static ShowEpisodeDetails MapEpisodeStub(JToken ep, int showId)
        {
            if (ep == null) return null;
            var season = ep.Value<int>("season_number");
            var episode = ep.Value<int>("episode_number");
            var airDate = ep.Value<string>("air_date");
            return new ShowEpisodeDetails
            {
                EpisodeFullNameNumber = $"S{season:00}E{episode:00} - {ep.Value<string>("name")}",
                EpisodeLink = EpisodeLink(showId, season, episode),
                EpisodeAirDate = string.IsNullOrEmpty(airDate) ? "Unknown" : airDate
            };
        }

        private static ShowEpisodeDetails MapEpisode(JToken ep, int showId, int season, string showTitle)
        {
            var episode = ep.Value<int>("episode_number");
            var airDate = ep.Value<string>("air_date");
            return new ShowEpisodeDetails
            {
                EpisodeShowTitle = showTitle,
                EpisodeShowLink = ShowLink(showId),
                EpisodeLink = EpisodeLink(showId, season, episode),
                EpisodeFullNameNumber = ep.Value<string>("name"),
                EpisodeNumber = $"S{season:00} - E{episode:00}",
                EpisodeAirDate = string.IsNullOrEmpty(airDate) ? "Unknown" : airDate,
                EpisodeSummary = "Summary: " + (ep.Value<string>("overview") ?? string.Empty),
                EpisodeImage = Image(ep.Value<string>("still_path"), StillSize),
                // TMDB is a metadata source only -- it has no streaming links to offer here.
                IsEpisodeWatchable = false,
                EpisodeStreamLinks = null
            };
        }

        public static async Task<SeriesDetails> GetDetailsForTVShowSeries(string link)
        {
            if (!TryParseShowLink(link, out int id)) return null;
            var json = await GetJsonAsync($"/tv/{id}");
            if (json == null) return null;

            var genreNames = json["genres"]?.Select(g => g.Value<string>("name"));
            var details = new SeriesDetails
            {
                Title = json.Value<string>("name"),
                ImageLink = Image(json.Value<string>("poster_path"), PosterSize),
                SeriesLink = link,
                Description = json.Value<string>("overview"),
                ReleaseDate = json.Value<string>("first_air_date"),
                Genres = genreNames != null ? string.Join(", ", genreNames) : string.Empty,
                LastEpisode = MapEpisodeStub(json["last_episode_to_air"], id),
                NextEpisode = MapEpisodeStub(json["next_episode_to_air"], id),
                Seasons = new List<SeasonData>()
            };

            if (json["seasons"] != null)
            {
                foreach (var season in json["seasons"])
                {
                    var seasonNumber = season.Value<int>("season_number");
                    var seasonJson = await GetJsonAsync($"/tv/{id}/season/{seasonNumber}");
                    var episodes = seasonJson?["episodes"]?
                        .Select(e => MapEpisode(e, id, seasonNumber, details.Title))
                        .ToList() ?? new List<ShowEpisodeDetails>();

                    details.Seasons.Add(new SeasonData
                    {
                        SeasonName = season.Value<string>("name") ?? $"Season {seasonNumber}",
                        Episodes = episodes
                    });
                }
            }

            return details;
        }

        public static async Task<ShowEpisodeDetails> GetDetailsForTVShowEpisode(string link)
        {
            if (!TryParseEpisodeLink(link, out int id, out int season, out int episode)) return null;

            var showJson = await GetJsonAsync($"/tv/{id}");
            var episodeJson = await GetJsonAsync($"/tv/{id}/season/{season}/episode/{episode}");
            if (showJson == null || episodeJson == null) return null;

            var mapped = MapEpisode(episodeJson, id, season, showJson.Value<string>("name"));
            mapped.EpisodeFullNameNumber = $"{episodeJson.Value<string>("name")} (Season {season} - Episode {episode})";
            if (string.IsNullOrEmpty(mapped.EpisodeImage))
            {
                mapped.EpisodeImage = Image(showJson.Value<string>("poster_path"), PosterSize);
            }
            return mapped;
        }

        // ---------------- schedule ----------------

        /// <summary>Approximates the old site-scraped TV schedule using TMDB's "on the air" list,
        /// grouped by each show's next episode air date. Scans a few pages of on_the_air shows;
        /// increase pagesToScan for a fuller schedule at the cost of more requests.</summary>
        public static async Task<Dictionary<string, List<CalenderScheduleList>>> GetTVSchedule(int pagesToScan = 2)
        {
            var dates = new Dictionary<string, List<CalenderScheduleList>>();
            for (int page = 1; page <= pagesToScan; page++)
            {
                var json = await GetJsonAsync("/tv/on_the_air", new Dictionary<string, string> { ["page"] = page.ToString() });
                if (json?["results"] == null) break;

                foreach (var s in json["results"])
                {
                    var id = s.Value<int>("id");
                    var showJson = await GetJsonAsync($"/tv/{id}");
                    var next = showJson?["next_episode_to_air"];
                    if (next == null) continue;

                    var airDate = next.Value<string>("air_date");
                    if (string.IsNullOrEmpty(airDate)) continue;

                    var season = next.Value<int>("season_number");
                    var episode = next.Value<int>("episode_number");
                    var key = DateTime.TryParse(airDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
                        ? parsedDate.ToString("ddd, MMM d", CultureInfo.InvariantCulture)
                        : airDate;

                    if (!dates.TryGetValue(key, out var list))
                    {
                        list = new List<CalenderScheduleList>();
                        dates[key] = list;
                    }

                    list.Add(new CalenderScheduleList
                    {
                        ItemType = DataEnum.DataType.TVSchedule,
                        Title = s.Value<string>("name"),
                        ImageLink = Image(s.Value<string>("poster_path"), PosterSize),
                        PageLink = EpisodeLink(id, season, episode),
                        EpisodeNo = $"S{season:00}E{episode:00}",
                        EpisodeName = next.Value<string>("name")
                    });
                }

                var totalPages = json.Value<int>("total_pages");
                if (page >= totalPages) break;
            }
            return dates.Count > 0 ? dates : null;
        }
    }
}
