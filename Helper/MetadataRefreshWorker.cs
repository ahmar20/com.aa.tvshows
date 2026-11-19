using System.Threading.Tasks;
using Android.Content;
using AndroidX.Work;
using Java.Util.Concurrent;
using System;


namespace com.aa.tvshows.Helper
{
    public class MetadataRefreshWorker : Worker
    {
        public MetadataRefreshWorker(Context context, WorkerParameters workerParams)
            : base(context, workerParams)
        {
        }

        public override Result DoWork()
        {
            // Worker.DoWork runs on a background thread already, but it's synchronous —
            // block on the async call here.
            bool success = Task.Run(() => MetadataCache.RefreshAsync(ApplicationContext)).Result;
            return success ? Result.InvokeSuccess() : Result.InvokeRetry();
        }
    }

    public static class MetadataScheduler
    {
        private const string WorkName = "tmdb_metadata_refresh";

        public static void ScheduleDailyRefresh(Context ctx)
        {
            var constraints = new Constraints.Builder().SetRequiredNetworkType(NetworkType.Connected).Build();

            var request = new PeriodicWorkRequest.Builder(
                    typeof(MetadataRefreshWorker), TimeSpan.FromHours(24)).SetConstraints(constraints).Build();

            WorkManager.GetInstance(ctx).EnqueueUniquePeriodicWork(WorkName, ExistingPeriodicWorkPolicy.Keep, request);
        }
    }
}