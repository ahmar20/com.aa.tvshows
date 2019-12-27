using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;

namespace com.aa.tvshows.Helper
{
    public class TemplateData
    {

    }

    public class TitleFragment
    {
        public string Title { get; set; }
        public Fragment Fragmnet { get; set; }
    }

    /// <summary>
    /// Base class for TV Series
    /// </summary>
    public class EpisodeList
    {
        public string Title { get; set; }
        public string EpisodeNo { get; set; }
        public string ImageLink { get; set; }
        public string PageLink { get; set; }
        public DataEnum.DataType ItemType { get; set; }
    }

    /// <summary>
    /// Derived class for TV Series data
    /// </summary>
    public class ShowList : EpisodeList
    {
        public string EpisodeDetail { get; set; }
    }

    /// <summary>
    /// Derived class for TV Shows Schedule
    /// </summary>
    public class CalenderScheduleList : EpisodeList
    {
        public string EpisodeName { get; set; }
    }

    /// <summary>
    /// Derived class for TV Shows Search
    /// </summary>
    public class SearchList : ShowList
    {
        public int NextPage { get; set; }
    }

    /// <summary>
    /// Base class for TV Shows Genres
    /// </summary>
    public class GenresShow
    {
        public string Title { get; set; }
        public string PageLink { get; set; }
        public string GenreType { get; set; }
        public string ReleaseYear { get; set; }
    }

    /// <summary>
    /// Derived class for Episodes in a genre
    /// </summary>
    public class GenresEpisode : GenresShow
    {
        public string EpisodeDetail { get; set; }
    }

    /// <summary>
    /// Search related suggestions data
    /// </summary>
    public class SearchSuggestionsData
    {
        public string Title { get; set; }
        public string ItemLink { get; set; }
    }

    /// <summary>
    /// Includes detailed info about a TV Show
    /// </summary>
    public class SeriesDetails
    {
        public string Title { get; set; }
        public string ImageLink { get; set; }
        public string Description { get; set; }
        public string ReleaseDate { get; set; }
        public string Genres { get; set; }
        public ShowEpisodeDetails LastEpisode { get; set; }
        public ShowEpisodeDetails NextEpisode { get; set; }
        public List<SeasonData> Seasons { get; set; }
    }

    /// <summary>
    /// Includes data for TV Series Season and Episodes in that season
    /// </summary>
    public class SeasonData
    {
        public string SeasonName { get; set; }
        public List<ShowEpisodeDetails> Episodes { get; set; }
    }

    /// <summary>
    /// Includes info for TV Series Episode
    /// </summary>
    public class ShowEpisodeDetails
    {
        public string EpisodeShowTitle { get; set; }
        public string EpisodeSummary { get; set; }
        public string EpisodeImage { get; set; }
        public string EpisodeLink { get; set; }
        public string EpisodeFullNameNumber { get; set; }
        public string EpisodeAirDate { get; set; }
        public bool IsEpisodeWatchable { get; set; }
        public List<EpisodeStreamLink> EpisodeStreamLinks { get; set; }
        public string EpisodeNumber { get; set; }
    }

    /// <summary>
    /// Includes info for Episode Streaming Host
    /// </summary>
    public class EpisodeStreamLink
    {
        public string HostName { get; set; }
        public string HostUrl { get; set; }
        public string HostImage { get; set; }
    }

    public class StreamingUri
    {
        public Uri StreamingUrl { get; set; }
        public string StreamingQuality { get; set; }
        public string PosterUrl { get; set; }
    }
}