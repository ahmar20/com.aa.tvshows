using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Work;

namespace com.aa.tvshows.Helper
{
    public class ShowsNotificationService : Worker
    {
        static int NOTIFICATION_ID = 1000;
        static readonly string CHANNEL_ID = "episodes_notification";

        public ShowsNotificationService(Context context, WorkerParameters workerParameters) : base(context, workerParameters)
        {

        }

        public override Result DoWork()
        {
            CheckNotifcationsForFavorites();

            return Result.InvokeSuccess();
        }

        public async void CheckNotifcationsForFavorites()
        {
            var favList = await StorageData.GetSeriesListFromFavoritesFile();
            if (favList != null && favList.Count > 0)
            {
                foreach (SeriesDetails series in favList)
                {
                    if (series.NextEpisode != null)
                    {
                        if (await WebData.GetDetailsForTVShowSeries(series.SeriesLink) is SeriesDetails updatedSeries)
                        {
                            if (await StorageData.SaveSeriesToFavoritesFile(updatedSeries))
                            {
                                if (updatedSeries.NextEpisode != null && updatedSeries.NextEpisode.EpisodeStreamLinks != null &&
                                    updatedSeries.NextEpisode.EpisodeStreamLinks.Count > 0)
                                {
                                    await CreateNotificationChannel();

                                    var activityIntent = new Intent(this.ApplicationContext, typeof(EpisodeDetailActivity));
                                    activityIntent.PutExtra("itemLink", updatedSeries.NextEpisode.EpisodeLink);

                                    var stackBuilder = TaskStackBuilder.Create(this.ApplicationContext);
                                    stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(EpisodeDetailActivity)));
                                    stackBuilder.AddNextIntent(activityIntent);

                                    var resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)Android.App.PendingIntentFlags.UpdateCurrent);

                                    var builder = new NotificationCompat.Builder(this.ApplicationContext, CHANNEL_ID)
                                                .SetAutoCancel(true).SetContentIntent(resultPendingIntent)
                                                .SetContentTitle($"New Episode for {updatedSeries.Title}").SetSmallIcon(Resource.Drawable.icon)
                                                .SetContentText($"{updatedSeries.NextEpisode.EpisodeFullNameNumber}");
                                    var notificationManager = NotificationManagerCompat.From(this.ApplicationContext);
                                    notificationManager.Notify(NOTIFICATION_ID++, builder.Build());
                                }
                            }
                        }
                    }
                }
            }
        }

        private Task CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return Task.CompletedTask;
            }

            var name = "TV_Shows_Notification_Channel";
            var description = "Notification channel for showing new episodes notifications";
            var channel = new Android.App.NotificationChannel(CHANNEL_ID, name, Android.App.NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (Android.App.NotificationManager)Android.App.Service.NotificationService;
            notificationManager.CreateNotificationChannel(channel);

            return Task.CompletedTask;
        }
    }
}