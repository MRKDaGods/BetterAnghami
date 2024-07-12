using DiscordRPC;
using MRK.Models;
using System.Text.RegularExpressions;

namespace MRK
{
    public partial class AnghamiRPC
    {
        private const string AppId = "1180106494042185778";

        private readonly DiscordRpcClient _client;

        /// <summary>
        /// Is our client initialized and running?
        /// </summary>
        public bool IsInitialized => _client.IsInitialized;

        private static AnghamiRPC? _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static AnghamiRPC Instance
        {
            get => _instance ??= new AnghamiRPC();
        }

        public AnghamiRPC()
        {
            // create client
            _client = new DiscordRpcClient(AppId);
        }

        /// <summary>
        /// Attempts to initialize the Discord RPC client
        /// </summary>
        public bool Initialize()
        {
            return !_client.IsInitialized && _client.Initialize();
        }

        /// <summary>
        /// Sets a new presence with the provided song details
        /// </summary>
        /// <param name="remainingTime">Remaining time in seconds</param>
        public void SetSong(Song song, int imageSize = 512)
        {
            if (!IsInitialized)
            {
                return;
            }

            // if remainingTime is available, make use of it
            Timestamps? ts = null;
            if (song.RemainingTime > 0 && song.IsPlaying)
            {
                ts = Timestamps.FromTimeSpan(song.RemainingTime);
            }

            var stateText = $"by {song.Artist}";
            if (!song.IsPlaying)
            {
                stateText = $"{song.SongPlayStatus.ToString().ToUpper()} - {stateText}";
            }

            // build presence
            var presence = new RichPresence()
                .WithTimestamps(ts)
                .WithDetails(FixStringForRPC(song.Name))
                .WithState(FixStringForRPC(stateText))
                .WithAssets(new Assets
                {
                    LargeImageKey = ImageSizeRegex().Replace(song.ImgUrl, $"&size={imageSize}"),
                    LargeImageText = FixStringForRPC(song.Name),
                });

            presence.Buttons =
            [
                new Button
                {
                    Label = "Play on ANGHAMI",
                    Url = $"https://play.anghami.com/song/{song.Id}"
                }
            ];

            // send to discord
            _client.SetPresence(presence);
        }

        /// <summary>
        /// Clears the current presence (if exists)
        /// </summary>
        public void Clear()
        {
            if (IsInitialized)
            {
                _client.ClearPresence();
            }
        }

        /// <summary>
        /// Shuts down the Discord RPC client
        /// </summary>
        public void Stop()
        {
            if (IsInitialized)
            {
                _client.Deinitialize();
            }
        }
        
        /// <summary>
        /// Ensures that a string satisfies the Discord RPC spec
        /// </summary>
        private static string FixStringForRPC(string str)
        {
            // trim
            str = str.Trim();

            // 128 byte check
            if (str.Length > 128)
            {
                // very long str...
                str = string.Concat(str.AsSpan(0, 125), "...");
            }

            return str;
        }

        [GeneratedRegex(@"&size=\d+")]
        private static partial Regex ImageSizeRegex();
    }
}
