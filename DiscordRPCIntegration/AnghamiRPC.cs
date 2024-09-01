using DiscordRPC;
using MRK.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace MRK
{
    public partial class AnghamiRPC
    {
        private const string AppId = "1180106494042185778";

        /// <summary>
        /// RPC thread sleep interval
        /// </summary>
        private const int RpcThreadInterval = 1000;

        /// <summary>
        /// Maximum time allowed for unsynchronization of RPC and realtime anghami song remaining time
        /// </summary>
        private const int MaxAllowedUnsynchronizedTime = 5;

        private readonly DiscordRpcClient _client;
        private readonly ISongHost _songHost;

        /// <summary>
        /// Is our client initialized and running?
        /// </summary>
        public bool IsInitialized => _client.IsInitialized;

        public AnghamiRPC(ISongHost songHost)
        {
            // create client
            _client = new DiscordRpcClient(AppId);

            // set song host
            _songHost = songHost;
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
                .WithType(ActivityType.Listening)
                .WithTimestamps(ts)
                .WithDetails(FixStringForRPC(song.Name))
                .WithState(FixStringForRPC(stateText))
                .WithAssets(new Assets
                {
                    LargeImageKey = ImageSizeRegex().Replace(song.ImgUrl, $"&size={imageSize}"),
                    LargeImageText = FixStringForRPC(song.Name),
                });

            // display play button for non local files
            if (song.Id >= 0)
            {
                presence.Buttons = [
                    new Button {
                        Label = "Play on ANGHAMI",
                        Url = $"https://play.anghami.com/song/{song.Id}"
                    }
                ];
            }

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
            var utf8 = Encoding.UTF8;
            if (utf8.GetByteCount(str) > 128)
            {
                // GetString adds an extra 2 bytes for some reason, so copy first 123 bytes from original string

                // very long str...
                str = string.Concat(
                    utf8.GetString(new ArraySegment<byte>(utf8.GetBytes(str), 0, 123)),
                    "...");
            }

            return str;
        }

        /// <summary>
        /// RPC thread loop
        /// </summary>
        private void RpcThread()
        {
            // keep track of last set song
            Song? lastSetSong = null;
            string? lastSetPlayState = null;    // play state of last sent RPC
            int lastRemainingTime = 0;          // remaining time of last sent RPC

            while (IsInitialized && _songHost.IsRunning)
            {
                // check song
                var song = _songHost.GetCurrentlyPlayingSong();
                if (song != lastSetSong || (song != null && (lastSetPlayState != song.PlayState
                                                             || Math.Abs(song.RemainingTime - lastRemainingTime) > MaxAllowedUnsynchronizedTime)))
                {
                    if (song != null)
                    {
                        // update local states
                        lastSetPlayState = song.PlayState;
                        lastRemainingTime = song.RemainingTime;

                        // update rpc
                        SetSong(song);
                    }
                    else
                    {
                        // clear everything
                        Clear();

                        lastSetPlayState = null;
                        lastRemainingTime = 0;
                    }

                    lastSetSong = song;
                }

                Thread.Sleep(RpcThreadInterval);
            }
        }

        /// <summary>
        /// Starts a new thread running the discord rpc loop
        /// </summary>
        public void StartRpcThread()
        {
            // start thread
            new Thread(RpcThread)
                .Start();
        }

        [GeneratedRegex(@"&size=\d+")]
        private static partial Regex ImageSizeRegex();
    }
}
