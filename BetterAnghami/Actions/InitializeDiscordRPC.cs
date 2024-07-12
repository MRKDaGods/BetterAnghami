using Microsoft.Web.WebView2.Core;
using MRK.Models;
using System;
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
        private const int RpcThreadInterval = 1000;

        /// <summary>
        /// Maximum time allowed for unsynchronization of RPC and realtime anghami song remaining time
        /// </summary>
        private const int MaxAllowedUnsynchronizedTime = 5;

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
            string? lastSetPlayState = null;    // play state of last sent RPC
            int lastRemainingTime = 0;          // remaining time of last sent RPC

            while (RPC.IsInitialized && anghWindow.IsRunning)
            {
                // check song
                Song? song = anghWindow.Dispatcher.Invoke(() => anghWindow.GetCurrentlyPlayingSong()).GetAwaiter().GetResult();
                if (song != lastSetSong || (song != null && (lastSetPlayState != song.PlayState
                                                             || Math.Abs(song.RemainingTime - lastRemainingTime) > MaxAllowedUnsynchronizedTime)))
                {
                    if (song != null)
                    {
                        // update local states
                        lastSetPlayState = song.PlayState;
                        lastRemainingTime = song.RemainingTime;

                        // update rpc
                        RPC.SetSong(song);
                    }
                    else
                    {
                        // clear everything
                        RPC.Clear();

                        lastSetPlayState = null;
                        lastRemainingTime = 0;
                    }

                    lastSetSong = song;
                }

                Thread.Sleep(RpcThreadInterval);
            }
        }
    }
}
