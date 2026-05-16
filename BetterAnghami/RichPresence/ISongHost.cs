using MRK.Models;

namespace MRK
{
    public interface ISongHost
    {
        /// <summary>
        /// Is host currently running?
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the currently playing song
        /// </summary>
        Song? GetCurrentlyPlayingSong();
    }
}
