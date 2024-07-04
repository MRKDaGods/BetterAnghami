using DiscordRPC;

namespace MRK
{
    public class AnghamiRPC
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
        public void SetSong(int id, string name, string artist, string imgUrl)
        {
            if (!IsInitialized)
            {
                return;
            }

            var presence = new RichPresence()
                .WithTimestamps(Timestamps.Now)
                .WithDetails(name)
                .WithState($"by {artist}")
                .WithAssets(new Assets
                {
                    LargeImageKey = imgUrl,
                    LargeImageText = name,
                });

            presence.Buttons =
            [
                new Button
                {
                    Label = "Play on ANGHAMI",
                    Url = $"https://play.anghami.com/song/{id}"
                }
            ];

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
    }
}
