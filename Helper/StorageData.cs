using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;

namespace com.aa.tvshows.Helper
{
    public static class StorageData
    {
        private static readonly Context DataContext = Application.Context;
        private static readonly string GetAppDataPath = System.Environment.GetFolderPath
            (System.Environment.SpecialFolder.Personal, System.Environment.SpecialFolderOption.Create);
        private const string FavoritesFileName = "UserFavorites.json";
        private static readonly string FavoritesFilePath = GetAppDataPath + "/" + FavoritesFileName;
        private static readonly string InternalStoragePath = DataContext.FilesDir.AbsolutePath + "/" + DataContext.Resources.GetString(Resource.String.app_name);

        public static async Task<bool> IsMarkedFavorite(SeriesDetails series)
        {
            if (series is null) return false;

            if (await GetFavoriteSeriesDataForKey(series.SeriesLink) is SeriesDetails) return true;
            return false;
        }

        public static async Task<List<SeriesDetails>> GetSeriesListFromFavoritesFile()
        {
            if (await GetFavoritesFileData() is string fileData)
            {
                try
                {
                    if (JsonConvert.DeserializeObject<List<SeriesDetails>>(fileData) is List<SeriesDetails> seriesList)
                    {
                        return seriesList.OrderBy(a => a.Title).ToList();
                    }
                }
                catch(JsonReaderException)
                {
                    Error.Instance.ShowErrorTip("Favorites data is corrupt. Emptying file...", DataContext);
                    await SaveFavoritesFileData("");
                }
                catch(Exception e)
                {
                    Error.Instance.ShowErrorTip("Favorites loading error: " + e.Message, DataContext);
                }
            }
            return null;
        }

        public static async Task<bool> RemoveSeriesFromFavoritesFile(SeriesDetails series)
        {
            if (series is null) return false;

            List<SeriesDetails> seriesList;
            if (await GetSeriesListFromFavoritesFile() is List<SeriesDetails> savedSeries)
            {
                seriesList = savedSeries;
            }
            else
            {
                seriesList = new List<SeriesDetails>();
            }
            if (seriesList.Remove(seriesList.Where(a => a.SeriesLink == series.SeriesLink).FirstOrDefault()))
            {
                if (await SaveFavoritesFileData(JsonConvert.SerializeObject(seriesList), FavoritesFilePath))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<bool> ExportSavedFavorites()
        {
            if (await GetFavoritesFileData() is string favoritesFile)
            {
                try
                {
                    if (!Directory.Exists(InternalStoragePath))
                    {
                        Directory.CreateDirectory(InternalStoragePath);
                    }
                    return await SaveFavoritesFileData(favoritesFile, InternalStoragePath + "/" + FavoritesFileName);
                }
                catch(Exception e)
                {
                    Error.Instance.ShowErrorTip(e.Message, DataContext);
                    return false;
                }
            }
            else
            {
                Error.Instance.ShowErrorTip("No favorites were found to export.", DataContext);
                return false;
            }
        }

        public static async Task<bool> ImportSavedFavoritesFromFile()
        {
            return false;
        }

        private static async Task<SeriesDetails> GetFavoriteSeriesDataForKey(string seriesLink)
        {
            if (string.IsNullOrEmpty(seriesLink)) return null;

            if (await GetSeriesListFromFavoritesFile() is List<SeriesDetails> seriesData)
            {
                return seriesData.Where(a => a.SeriesLink == seriesLink).FirstOrDefault();
            }
            return null;
        }

        private static async Task<string> GetFavoritesFileData()
        {
            try
            {
                if (File.Exists(FavoritesFilePath))
                {
                    return await File.ReadAllTextAsync(FavoritesFilePath);
                }
            }
            catch (Exception ex)
            {
                Error.Instance.ShowErrorTip(ex.Message, DataContext);
            }

            return null;
        }

        public static async Task<bool> SaveSeriesToFavoritesFile(SeriesDetails series)
        {
            if (series is null) return false;

            List<SeriesDetails> seriesList;
            if (await GetSeriesListFromFavoritesFile() is List<SeriesDetails> savedSeries)
            {
                seriesList = savedSeries;
            }
            else
            {
                seriesList = new List<SeriesDetails>();
            }
            if (seriesList.Where(a => a.SeriesLink == series.SeriesLink).FirstOrDefault() is SeriesDetails saved)
            {
                // already here
                if (!seriesList.Remove(saved)) return false;
                seriesList.Add(series);
            }
            else
            {
                seriesList.Add(series);
            }
            if (!await SaveFavoritesFileData(JsonConvert.SerializeObject(seriesList), FavoritesFilePath))
            {
                return false;
            }
            return true;
        }

        private static async Task<bool> SaveFavoritesFileData(string data, string path = default)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path)) path = FavoritesFilePath;

                if (File.Exists(path))
                {
                    await File.WriteAllTextAsync(path, data);
                }
                else
                {
                    using var writer = File.CreateText(path);
                    await writer.WriteAsync(data);
                }
                return true;
            }
            catch (Exception ex)
            {
                Error.Instance.ShowErrorTip(ex.Message, DataContext);
            }

            return false;
        }
    }
}