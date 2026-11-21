using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.aa.tvshows.Models
{
    public class TmdbConfiguration
    {
        [JsonProperty("images")]
        public ImagesConfig Images { get; set; }
    }

    public class ImagesConfig
    {
        [JsonProperty("secure_base_url")]
        public string SecureBaseUrl { get; set; }

        [JsonProperty("backdrop_sizes")]
        public List<string> BackdropSizes { get; set; }

        [JsonProperty("poster_sizes")]
        public List<string> PosterSizes { get; set; }

        [JsonProperty("profile_sizes")]
        public List<string> ProfileSizes { get; set; }
    }

    public class TmdbGenre
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class TmdbGenreListResponse
    {
        [JsonProperty("genres")]
        public List<TmdbGenre> Genres { get; set; }
    }

    public class TmdbCountry
    {
        [JsonProperty("iso_3166_1")]
        public string Code { get; set; }

        [JsonProperty("english_name")]
        public string Name { get; set; }
    }
}