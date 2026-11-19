using System.Collections.Generic;
using com.aa.tvshows.Helper;

namespace com.aa.tvshows.Models
{
    public enum CardStyle
    {
        Poster,
        Wide
    }

    /// <summary>
    /// One tile in the new browsing UI (home rows, search results, genre grid, My List).
    /// Carries both the poster and backdrop path so the phone UI can use the poster as its hero
    /// and the TV UI can use the backdrop, without a second request.
    /// </summary>
    public class MediaCard
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string PosterPath { get; set; }
        public string BackdropPath { get; set; }
        public string Overview { get; set; }
        public string Year { get; set; }
        public string Rating { get; set; } = string.Empty;
        public string Genres { get; set; } = string.Empty;
        public string PageLink { get; set; }

        /// <summary>Small pill drawn on the tile, e.g. "NEW EPISODE" or "NEXT NOV 28".</summary>
        public string Badge { get; set; }

        /// <summary>The object this tile was built from (e.g. the saved SeriesDetails for My List).</summary>
        public object Source { get; set; }

        /// <summary>Ready-made image URLs (saved favorites store full URLs rather than TMDB paths).</summary>
        public string PosterUrlOverride { get; set; }
        public string BackdropUrlOverride { get; set; }

        public string Poster(string size = "w342") =>
            !string.IsNullOrEmpty(PosterUrlOverride) ? PosterUrlOverride : TMDbClient.ImageUrl(PosterPath, size);

        public string Backdrop(string size = "w780") =>
            !string.IsNullOrEmpty(BackdropUrlOverride) ? BackdropUrlOverride : TMDbClient.ImageUrl(BackdropPath, size);

    }

    /// <summary>One horizontal row on the home screen.</summary>
    public class HomeRow
    {
        public string Title { get; set; }
        public CardStyle Style { get; set; }
        public List<MediaCard> Items { get; set; } = new List<MediaCard>();

        /// <summary>The TMDB path and query this row was built from, so "View All" can page through the same listing.</summary>
        public string Endpoint { get; set; }
        public Dictionary<string, string> Query { get; set; }
    }

    /// <summary>One page of tiles from a paged TMDB endpoint.</summary>
    public class CardPage
    {
        public List<MediaCard> Items { get; set; } = new List<MediaCard>();
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public bool HasMore => Page < TotalPages;
    }
}
