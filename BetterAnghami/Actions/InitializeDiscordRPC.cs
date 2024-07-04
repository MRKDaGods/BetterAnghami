using Microsoft.Web.WebView2.Core;
using MRK.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MRK.Actions
{
    /// <summary>
    /// Ensures initialization of Discord RPC, and starts the RPC thread
    /// </summary>
    public class InitializeDiscordRPC(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        /// <summary>
        /// RPC thread sleep interval
        /// </summary>
        private const int RpcThreadInterval = 200;

        /// <summary>
        /// Has the RPC client been initialized?
        /// </summary>
        private bool _initialized = false;

        public override bool WaitForLoad => true;

        /// <summary>
        /// Anghami Discord RPC public instance
        /// </summary>
        private static AnghamiRPC RPC => AnghamiRPC.Instance;

        public override Task Execute()
        {
            if (!_initialized)
            {
                // attempt initialization
                _initialized = RPC.Initialize();

                if (_initialized)
                {
                    // start thread
                    new Thread(RpcThread)
                        .Start();
                }
            }

            return Task.CompletedTask;
        }

        public override bool ShouldConsume()
        {
            // only consume if successfully initialized
            return _initialized;
        }

        /// <summary>
        /// RPC thread loop
        /// </summary>
        private void RpcThread()
        {
            var anghWindow = AnghamiWindow.Instance;

            // keep track of last set song
            Song? lastSetSong = null;

            while (RPC.IsInitialized && anghWindow.IsRunning)
            {
                // TODO: check play state, and update RPC timestamps accordingly

                // check song
                Song? song = anghWindow.Dispatcher.Invoke(() => anghWindow.GetCurrentlyPlayingSong()).GetAwaiter().GetResult();
                if (song != lastSetSong)
                {
                    if (song != null)
                    {
                        // update rpc
                        RPC.SetSong(
                            song.Id,
                            song.Name,
                            song.Artist,
                            song.ImgUrl.Replace("&size=60", "&size=512"));
                    }
                    else
                    {
                        RPC.Clear();
                    }

                    lastSetSong = song;
                }

                Thread.Sleep(RpcThreadInterval);
            }
        }
    }
}
