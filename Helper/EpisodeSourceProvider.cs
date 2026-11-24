using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.aa.tvshows.Helper
{
    /// <summary>
    /// Where episode playback sources come from. TMDB itself is metadata-only and has no stream
    /// URLs -- this is the seam where a real source (a scraper, a personal media server, another
    /// API) gets wired in later. Everything downstream -- PlayerActivity's ExoPlayer setup, the
    /// external-media-player chooser, the Play button on each episode row -- already expects
    /// exactly this shape, so dropping in a real implementation here is the only change needed;
    /// nothing else has to be touched.
    /// </summary>
    public static class EpisodeSourceProvider
    {
        /// <summary>The playable qualities for an episode, or an empty list if none are available.
        /// Always empty today -- there is no backend wired up yet.</summary>
        public static Task<List<StreamingUri>> GetStreamsAsync(ShowEpisodeDetails episode)
        {
            // TODO: replace this with a real lookup once a streaming source exists. Keep the
            // return shape -- a List<StreamingUri>, each with StreamingQuality and StreamingUrl --
            // and PlayerActivity / ContextMenuDialog need no changes at all.
            return Task.FromResult(new List<StreamingUri>());
        }
    }
}
