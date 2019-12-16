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

    public class EpisodeList
    {
        public string Title { get; set; }
        public string EpisodeNo { get; set; }
        public string ImageLink { get; set; }
        public string PageLink { get; set; }
        public DataEnum.MainTabsType ItemType { get; set; }
    }

    public class ShowList : EpisodeList
    {
        public string EpisodeDetail { get; set; }
    }

    public class CalenderScheduleList : EpisodeList
    {
        public string EpisodeName { get; set; }
    }

    public class GenresShow
    {
        public string Title { get; set; }
        public string PageLink { get; set; }
        public string GenreType { get; set; }
        public string ReleaseYear { get; set; }
    }


    public class GenresEpisode : GenresShow
    {
        public string EpisodeDetail { get; set; }
    }
}