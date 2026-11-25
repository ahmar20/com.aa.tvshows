using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using com.aa.tvshows.Models;
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
        // TMDB only serves stills in w92 / w185 / w300 / original
        const string StillSize = "w300";
        const string StillLargeSize = "original";
        const string BackdropLargeSize = "w1280";
        const double RequestTimeoutSeconds = 15;

        public const string LinkScheme = "tmdb://tv/";

        static HttpClient _client;
        static HttpClient Client => _client ??= new HttpClient();

        static Task<Dictionary<int, string>> _genreTask;
        static string _imageBase;

        /// <summary>
        /// Builds a full TMDB image URL. The base URL comes from the daily-refreshed
        /// /configuration cache (MetadataCache) once it has been populated, and falls back
        /// to the well-known default until then.
        /// </summary>
        public static string ImageUrl(string path, string size)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_imageBase == null)
            {
                var cached = MetadataCache.GetCachedConfiguration(Application.Context)?.Images?.SecureBaseUrl;
                _imageBase = string.IsNullOrEmpty(cached) ? ImageBase : cached;
            }
            return _imageBase + size + path;
        }

        private static async Task<JObject> GetJsonAsync(string path, Dictionary<string, string> query = null)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                Error.Instance.ShowErrorTip("Set TMDbClient.ApiKey with your TMDB API key before browsing shows.", Application.Context);
                return null;
            }
            try
            {
                var qs = $"language=en-US&region={StorageData.GetRegion()}";
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

        static string Image(string path, string size) => ImageUrl(path, size);

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

        // One shared task so the many parallel home-screen requests don't each fetch the genre list.
        private static Task<Dictionary<int, string>> GetGenreMap() => _genreTask ??= LoadGenreMapAsync();

        private static async Task<Dictionary<int, string>> LoadGenreMapAsync()
        {
            // the daily MetadataCache refresh already keeps the genre list on disk
            var persisted = MetadataCache.GetCachedGenres(Application.Context);
            if (persisted != null && persisted.Count > 0)
            {
                return persisted.GroupBy(g => g.Id).ToDictionary(g => g.Key, g => g.First().Name);
            }

            var json = await GetJsonAsync("/genre/tv/list").ConfigureAwait(false);
            if (json?["genres"] == null)
            {
                _genreTask = null; // let the next caller retry
                return new Dictionary<int, string>();
            }
            return json["genres"].ToDictionary(g => g.Value<int>("id"), g => g.Value<string>("name"));
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
            // TMDB sends an explicit JSON null for shows with no last/next episode (every ended show)
            if (ep == null || ep.Type == JTokenType.Null) return null;
            var season = ep.Value<int>("season_number");
            var episode = ep.Value<int>("episode_number");
            var airDate = ep.Value<string>("air_date");
            return new ShowEpisodeDetails
            {
                ShowTmdbId = showId.ToString(),
                SeasonNumber = season,
                EpisodeFullNameNumber = $"S{season:00}E{episode:00} - {ep.Value<string>("name")}",
                EpisodeLink = EpisodeLink(showId, season, episode),
                EpisodeAirDate = string.IsNullOrEmpty(airDate) ? "Unknown" : airDate
            };
        }

        private static ShowEpisodeDetails MapEpisode(JToken ep, SeasonData season)
        {
            var showId = int.Parse(season.ShowTmdbId);
            var episode = ep.Value<int>("episode_number");
            var airDate = ep.Value<string>("air_date");
            var runtime = ep.Value<int?>("runtime") ?? 0;
            var stillPath = ep.Value<string>("still_path");
            return new ShowEpisodeDetails
            {
                ShowTmdbId = season.ShowTmdbId,
                SeasonNumber = season.SeasonNumber,
                EpisodeShowTitle = season.ShowTitle,
                EpisodeShowLink = ShowLink(showId),
                EpisodeLink = EpisodeLink(showId, season.SeasonNumber, episode),
                EpisodeFullNameNumber = ep.Value<string>("name"),
                EpisodeNumber = $"S{season.SeasonNumber:00} - E{episode:00}",
                EpisodeIndex = episode,
                EpisodeAirDate = string.IsNullOrEmpty(airDate) ? "Unknown" : airDate,
                EpisodeSummary = ep.Value<string>("overview") ?? string.Empty,
                EpisodeRuntime = runtime > 0 ? $"{runtime}m" : string.Empty,
                EpisodeImage = Image(stillPath, StillSize),
                EpisodeImageLarge = Image(stillPath, StillLargeSize)
            };
        }

        /// <summary>
        /// Loads a show. With <paramref name="includeEpisodes"/> false only the show-level data and the
        /// season list are fetched (one request) and each season's episodes are left null; use
        /// <see cref="GetSeasonEpisodes"/> to fill a season in when it is opened. Saving to My List and
        /// the favorites refresh still ask for everything (the default).
        /// </summary>
        public static async Task<SeriesDetails> GetDetailsForTVShowSeries(string link, bool includeEpisodes = true)
        {
            if (!TryParseShowLink(link, out int id)) return null;
            // append_to_response folds TMDB's /tv/{id}/external_ids (imdb_id, tvdb_id, etc.) into
            // this same response instead of needing a second round-trip.
            var json = await GetJsonAsync($"/tv/{id}", new Dictionary<string, string> { ["append_to_response"] = "external_ids" });
            if (json == null) return null;

            var genreNames = json["genres"]?.Select(g => g.Value<string>("name"));
            var rating = json.Value<double?>("vote_average") ?? 0;
            var details = new SeriesDetails
            {
                TmdbId = id.ToString(),
                Title = json.Value<string>("name"),
                ImageLink = Image(json.Value<string>("poster_path"), PosterSize),
                BackdropLink = Image(json.Value<string>("backdrop_path"), BackdropLargeSize),
                SeriesLink = link,
                Description = json.Value<string>("overview"),
                ReleaseDate = json.Value<string>("first_air_date"),
                Genres = genreNames != null ? string.Join(", ", genreNames) : string.Empty,
                Rating = rating > 0 ? rating.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty,
                Status = json.Value<string>("status"),
                ImdbId = json["external_ids"]?.Value<string>("imdb_id"),
                LastEpisode = MapEpisodeStub(json["last_episode_to_air"], id),
                NextEpisode = MapEpisodeStub(json["next_episode_to_air"], id),
                Seasons = new List<SeasonData>()
            };

            if (json["seasons"] != null)
            {
                // "Specials" (season 0) goes last so Season 1 is the first thing people see
                foreach (var season in json["seasons"].OrderBy(s => s.Value<int>("season_number") == 0 ? 1 : 0)
                                                      .ThenBy(s => s.Value<int>("season_number")))
                {
                    var seasonNumber = season.Value<int>("season_number");
                    details.Seasons.Add(new SeasonData
                    {
                        ShowTmdbId = details.TmdbId,
                        ShowTitle = details.Title,
                        SeasonNumber = seasonNumber,
                        SeasonName = season.Value<string>("name") ?? $"Season {seasonNumber}",
                        EpisodeCount = season.Value<int?>("episode_count") ?? 0,
                        Episodes = null
                    });
                }

                if (includeEpisodes)
                {
                    // all seasons in parallel instead of one request after the other
                    await Task.WhenAll(details.Seasons.Select(async s =>
                    {
                        s.Episodes = await GetSeasonEpisodes(s).ConfigureAwait(false)
                                     ?? new List<ShowEpisodeDetails>();
                    })).ConfigureAwait(false);
                }
            }

            return details;
        }

        public static async Task<string> GetImdbId(string tmdbId)
        {
            if (string.IsNullOrWhiteSpace(tmdbId)) return null;
            var json = await GetJsonAsync($"/tv/{tmdbId}", new Dictionary<string, string> { ["append_to_response"] = "external_ids" });
            if (json == null) return null;

            return json["external_ids"]?.Value<string>("imdb_id");
        }

        /// <summary>The episodes of one season, or null if the request failed.</summary>
        public static async Task<List<ShowEpisodeDetails>> GetSeasonEpisodes(SeasonData season)
        {
            var seasonJson = await GetJsonAsync($"/tv/{season.ShowTmdbId}/season/{season.SeasonNumber}").ConfigureAwait(false);
            if (seasonJson?["episodes"] == null) return null;
            return seasonJson["episodes"].Select(e => MapEpisode(e, season)).ToList();
        }

        public static async Task<ShowEpisodeDetails> GetDetailsForTVShowEpisode(string link)
        {
            if (!TryParseEpisodeLink(link, out int id, out int season, out int episode)) return null;

            var showTask = GetJsonAsync($"/tv/{id}");
            var episodeTask = GetJsonAsync($"/tv/{id}/season/{season}/episode/{episode}");
            await Task.WhenAll(showTask, episodeTask);
            var showJson = showTask.Result;
            var episodeJson = episodeTask.Result;
            if (showJson == null || episodeJson == null) return null;

            var seasonForMap = new SeasonData
            {
                ShowTmdbId = id.ToString(),
                ShowTitle = showJson.Value<string>("name"),
                SeasonNumber = season
            };
            var mapped = MapEpisode(episodeJson, seasonForMap);
            mapped.EpisodeFullNameNumber = episodeJson.Value<string>("name");
            if (string.IsNullOrEmpty(mapped.EpisodeImage))
            {
                // unaired episodes have no still yet -- fall back to the show's own artwork
                mapped.EpisodeImage = Image(showJson.Value<string>("backdrop_path"), "w780")
                                      ?? Image(showJson.Value<string>("poster_path"), PosterSize);
                mapped.EpisodeImageLarge = Image(showJson.Value<string>("backdrop_path"), BackdropLargeSize) ?? mapped.EpisodeImage;
            }
            return mapped;
        }

        // ---------------- schedule ----------------

        /// <summary>Approximates the old site-scraped TV schedule using TMDB's "on the air" list, grouped by
        /// air date (soonest day first), within [today - <paramref name="daysBack"/>, today + <paramref name="daysForward"/>].
        /// Both a show's last aired episode and its next one are considered, so days in the past can show
        /// something too. The per-show lookups run in parallel (a handful at a time) so the screen doesn't
        /// wait for dozens of sequential requests.</summary>
        public static async Task<Dictionary<string, List<CalenderScheduleList>>> GetTVSchedule(int daysBack = 1, int daysForward = 7, int pagesToScan = 5)
        {
            var pageJson = await Task.WhenAll(Enumerable.Range(1, pagesToScan).Select(page =>
                GetJsonAsync("/tv/on_the_air", new Dictionary<string, string> { ["page"] = page.ToString() })));

            var shows = pageJson.Where(p => p?["results"] != null).SelectMany(p => p["results"]).ToList();
            if (shows.Count == 0) return null;

            var today = DateTime.Today;
            var windowStart = today.AddDays(-daysBack);
            var windowEnd = today.AddDays(daysForward);

            using var gate = new SemaphoreSlim(8);
            var lookups = await Task.WhenAll(shows.Select(async show =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    var details = await GetJsonAsync($"/tv/{show.Value<int>("id")}").ConfigureAwait(false);
                    return (Show: show, Details: details);
                }
                finally
                {
                    gate.Release();
                }
            }));

            var byDate = new SortedDictionary<DateTime, List<CalenderScheduleList>>();
            void AddIfInWindow(JToken show, JToken episode)
            {
                if (episode == null || episode.Type == JTokenType.Null) return;
                var airDate = episode.Value<string>("air_date");
                if (!DateTime.TryParse(airDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)) return;
                if (parsedDate.Date < windowStart || parsedDate.Date > windowEnd) return;

                var id = show.Value<int>("id");
                var season = episode.Value<int>("season_number");
                var episodeNumber = episode.Value<int>("episode_number");

                if (!byDate.TryGetValue(parsedDate.Date, out var list))
                {
                    list = new List<CalenderScheduleList>();
                    byDate[parsedDate.Date] = list;
                }
                // a show can contribute both its last and next episode if both fall in the window;
                // skip an exact duplicate (same show, same date) rather than list it twice
                var title = show.Value<string>("name");
                if (list.Any(a => a.Title == title)) return;

                list.Add(new CalenderScheduleList
                {
                    ItemType = DataEnum.DataType.TVSchedule,
                    Title = title,
                    ImageLink = Image(show.Value<string>("poster_path"), PosterSize),
                    PageLink = EpisodeLink(id, season, episodeNumber),
                    EpisodeNo = $"S{season:00}E{episodeNumber:00}",
                    EpisodeName = episode.Value<string>("name")
                });
            }

            foreach (var (show, details) in lookups)
            {
                if (details == null) continue;
                AddIfInWindow(show, details["last_episode_to_air"]);
                AddIfInWindow(show, details["next_episode_to_air"]);
            }

            if (byDate.Count == 0) return null;

            var dates = new Dictionary<string, List<CalenderScheduleList>>();
            foreach (var kv in byDate)
            {
                var label = kv.Key == today ? "Today"
                          : kv.Key == today.AddDays(1) ? "Tomorrow"
                          : kv.Key == today.AddDays(-1) ? "Yesterday"
                          : kv.Key.ToString("dddd, MMM d", CultureInfo.InvariantCulture);
                dates[label] = kv.Value.OrderBy(a => a.Title, StringComparer.OrdinalIgnoreCase).ToList();
            }
            return dates;
        }

        // ---------------- card based listings (browse UI) ----------------

        // TMDB's own TV genre ids are stable; names are only a fallback for when the genre list is not cached yet.
        static readonly (int Id, string Name)[] HomeGenres =
        {
            (18, "Drama"), (35, "Comedy"), (10765, "Sci-Fi & Fantasy"), (80, "Crime"),
            (10759, "Action & Adventure"), (16, "Animation"), (9648, "Mystery"), (99, "Documentary")
        };

        private static MediaCard MapCard(JToken s, Dictionary<int, string> genres, string badge = null)
        {
            var id = s.Value<int>("id");
            var names = (s["genre_ids"]?.Select(g => (int)g) ?? Enumerable.Empty<int>())
                .Where(genres.ContainsKey).Select(g => genres[g]).Take(3);
            var rating = s.Value<double?>("vote_average") ?? 0;
            return new MediaCard
            {
                Id = id,
                Title = s.Value<string>("name"),
                PosterPath = s.Value<string>("poster_path"),
                BackdropPath = s.Value<string>("backdrop_path"),
                Overview = s.Value<string>("overview"),
                Year = YearOf(s.Value<string>("first_air_date")),
                Rating = rating > 0 ? rating.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty,
                Genres = string.Join(", ", names),
                PageLink = ShowLink(id),
                Badge = badge
            };
        }

        private static async Task<CardPage> GetCardPageAsync(string path, int page = 1, Dictionary<string, string> query = null, string badge = null)
        {
            if (page < 1) page = 1;
            var q = query == null ? new Dictionary<string, string>() : new Dictionary<string, string>(query);
            q["page"] = page.ToString();

            var jsonTask = GetJsonAsync(path, q);
            var genresTask = GetGenreMap();
            await Task.WhenAll(jsonTask, genresTask);

            var json = jsonTask.Result;
            if (json?["results"] == null) return null;
            var genres = genresTask.Result;

            return new CardPage
            {
                Page = page,
                TotalPages = json.Value<int?>("total_pages") ?? page,
                Items = json["results"].Select(s => MapCard(s, genres, badge)).ToList()
            };
        }

        /// <summary>The home screen's rows. Every request is started immediately (they run in parallel);
        /// await the tasks in order to show the rows top to bottom as they arrive. Each request also
        /// carries the endpoint/query/badge it was built from, so "View All" can replay it, paginated.</summary>
        public static List<HomeRowRequest> StartHomeRowRequests()
        {
            var rows = new List<HomeRowRequest>
            {
                new HomeRowRequest("Trending This Week", CardStyle.Poster, "/trending/tv/week", null, null),
                new HomeRowRequest("Popular", CardStyle.Poster, "/tv/popular", null, null),
                new HomeRowRequest("New Episodes Today", CardStyle.Wide, "/tv/airing_today", null, "NEW EPISODE"),
                new HomeRowRequest("Currently Airing", CardStyle.Poster, "/tv/on_the_air", null, null),
                new HomeRowRequest("Top Rated", CardStyle.Poster, "/tv/top_rated", null, null)
            };
            foreach (var genre in HomeGenres)
            {
                rows.Add(new HomeRowRequest(genre.Name, CardStyle.Poster, "/discover/tv",
                    new Dictionary<string, string> { ["with_genres"] = genre.Id.ToString(), ["sort_by"] = "popularity.desc" }, null));
            }
            return rows;
        }

        public static async Task<List<TmdbGenre>> GetGenres()
        {
            var map = await GetGenreMap();
            return map.Select(kv => new TmdbGenre { Id = kv.Key, Name = kv.Value })
                      .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static Task<CardPage> DiscoverByGenre(int genreId, int page = 1) =>
            GetCardPageAsync("/discover/tv", page, new Dictionary<string, string>
            {
                ["with_genres"] = genreId.ToString(),
                ["sort_by"] = "popularity.desc"
            });

        public static Task<CardPage> SearchShows(string query, int page = 1) =>
            GetCardPageAsync("/search/tv", page, new Dictionary<string, string> { ["query"] = query });

        /// <summary>Generic paged listing, for the "View All" screen replaying a home row's own endpoint/query.</summary>
        public static Task<CardPage> GetCardPage(string endpoint, int page, Dictionary<string, string> query, string badge = null) =>
            GetCardPageAsync(endpoint, page, query, badge);
    }

    public class HomeRowRequest
    {
        public string Title { get; }
        public CardStyle Style { get; }
        public string Endpoint { get; }
        public Dictionary<string, string> Query { get; }
        public string Badge { get; }
        public Task<CardPage> Data { get; }

        public HomeRowRequest(string title, CardStyle style, string endpoint, Dictionary<string, string> query, string badge)
        {
            Title = title;
            Style = style;
            Endpoint = endpoint;
            Query = query;
            Badge = badge;
            Data = TMDbClient.GetCardPage(endpoint, 1, query, badge);
        }
    }
}
