using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Android.Content;
using com.aa.tvshows.Models;
using Newtonsoft.Json;

namespace com.aa.tvshows.Helper
{
    public static class MetadataCache
    {
        private const string PrefsName = "tmdb_metadata_cache";
        private const string KeyGenres = "genres_json";
        private const string KeyConfig = "config_json";
        private const string KeyLastFetched = "last_fetched_utc_ticks";
        private const long RefreshIntervalTicks = TimeSpan.TicksPerHour * 24;

        private const string BaseUrl = "https://api.themoviedb.org/3";

        private static ISharedPreferences GetPrefs(Context ctx) =>
            ctx.GetSharedPreferences(PrefsName, FileCreationMode.Private);

        public static bool IsStale(Context ctx)
        {
            var prefs = GetPrefs(ctx);
            long lastTicks = prefs.GetLong(KeyLastFetched, 0);
            if (lastTicks == 0) return true;

            var last = new DateTime(lastTicks, DateTimeKind.Utc);
            return DateTime.UtcNow - last > TimeSpan.FromTicks(RefreshIntervalTicks);
        }

        public static List<TmdbGenre> GetCachedGenres(Context ctx)
        {
            var json = GetPrefs(ctx).GetString(KeyGenres, null);
            if (string.IsNullOrEmpty(json)) return new List<TmdbGenre>();
            return JsonConvert.DeserializeObject<List<TmdbGenre>>(json);
        }

        public static TmdbConfiguration GetCachedConfiguration(Context ctx)
        {
            var json = GetPrefs(ctx).GetString(KeyConfig, null);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<TmdbConfiguration>(json);
        }

        /// <summary>
        /// Builds a full image URL from a path like "/abc123.jpg".
        /// sizeIndex picks from the available size buckets (0 = smallest, last = "original").
        /// Falls back to "original" if the cache hasn't been populated yet.
        /// </summary>
        public static string BuildPosterUrl(Context ctx, string posterPath, string preferredSize = "w500")
        {
            if (string.IsNullOrEmpty(posterPath)) return null;
            var config = GetCachedConfiguration(ctx);
            if (config?.Images == null)
                return $"https://image.tmdb.org/t/p/{preferredSize}{posterPath}"; // safe fallback

            var size = config.Images.PosterSizes.Contains(preferredSize)
                ? preferredSize
                : config.Images.PosterSizes.LastOrDefault() ?? "original";

            return $"{config.Images.SecureBaseUrl}{size}{posterPath}";
        }

        public static string BuildBackdropUrl(Context ctx, string backdropPath, string preferredSize = "w780")
        {
            if (string.IsNullOrEmpty(backdropPath)) return null;
            var config = GetCachedConfiguration(ctx);
            if (config?.Images == null)
                return $"https://image.tmdb.org/t/p/{preferredSize}{backdropPath}";

            var size = config.Images.BackdropSizes.Contains(preferredSize)
                ? preferredSize
                : config.Images.BackdropSizes.LastOrDefault() ?? "original";

            return $"{config.Images.SecureBaseUrl}{size}{backdropPath}";
        }

        public static string GenreNameFor(Context ctx, int genreId)
        {
            var genres = GetCachedGenres(ctx);
            return genres.FirstOrDefault(g => g.Id == genreId)?.Name ?? string.Empty;
        }

        /// <summary>
        /// Fetches genres + configuration from TMDB and overwrites the cache.
        /// Call this from the WorkManager job, or directly if you want an immediate refresh.
        /// </summary>
        public static async Task<bool> RefreshAsync(Context ctx)
        {
            try
            {
                using var client = new HttpClient();
                // same v4 read-access token (Bearer header) that TMDbClient uses
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secrets.TMDB_Api_Key);

                var genresTask = client.GetStringAsync($"{BaseUrl}/genre/tv/list?language=en-US");
                var configTask = client.GetStringAsync($"{BaseUrl}/configuration");

                await Task.WhenAll(genresTask, configTask);

                var genreResponse = JsonConvert.DeserializeObject<TmdbGenreListResponse>(genresTask.Result);
                var config = JsonConvert.DeserializeObject<TmdbConfiguration>(configTask.Result);

                var prefs = GetPrefs(ctx);
                using var editor = prefs.Edit();
                editor.PutString(KeyGenres, JsonConvert.SerializeObject(genreResponse.Genres));
                editor.PutString(KeyConfig, JsonConvert.SerializeObject(config));
                editor.PutLong(KeyLastFetched, DateTime.UtcNow.Ticks);
                editor.Apply();

                return true;
            }
            catch
            {
                // Network failure, bad JSON, etc. — leave old cache in place and let the
                // next scheduled run (or next app open) try again.
                return false;
            }
        }

        /// <summary>
        /// Convenience: refresh only if stale, otherwise no-op.
        /// Good to call once on app startup (e.g. MainActivity.OnCreate / Application.OnCreate).
        /// </summary>
        public static async Task RefreshIfStaleAsync(Context ctx)
        {
            if (IsStale(ctx))
                await RefreshAsync(ctx);
        }
    }
}