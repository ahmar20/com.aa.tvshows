using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace com.aa.tvshows.Helper
{
    public static class DataEnum
    {
        public enum DataType
        {
            None,
            PopularShows,
            NewPopularEpisodes,
            NewEpisodes,
            TVSchedule,
            Genres,
            SearchSuggestions
        }

        public enum GenreDataType
        {
            LatestEpisodes,
            PopularEpisodes,
            Shows
        }
    }
}