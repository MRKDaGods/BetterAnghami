using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    /// <summary>
    /// Ensures initialization of Discord RPC, and starts the RPC thread
    /// </summary>
    public class InitializeDiscordRPC(CoreWebView2 webView, AnghamiRPC rpc)
        : AsyncConsumableAction(webView)
    {
        /// <summary>
        /// Has the RPC client been initialized?
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// Whether we've already logged an init failure (this action retries on every navigation)
        /// </summary>
        private bool _initFailureLogged;

        /// <summary>
        /// Anghami RPC instance
        /// </summary>
        private readonly AnghamiRPC _anghamiRpc = rpc;

        public override bool WaitForLoad => true;

        public override Task Execute()
        {
            if (!_initialized)
            {
                // attempt initialization
                _initialized = _anghamiRpc.Initialize();

                if (_initialized)
                {
                    Tracer.Info(Tracer.Category.Rpc, "Discord RPC initialized, starting thread");

                    // start thread
                    _anghamiRpc.StartRpcThread();
                }
                else if (!_initFailureLogged)
                {
                    // log once, not on every navigation
                    _initFailureLogged = true;
                    Tracer.Warn(
                        Tracer.Category.Rpc,
                        "Discord RPC init failed (Discord not detected), will retry on navigation"
                    );
                }
            }

            return Task.CompletedTask;
        }

        public override bool ShouldConsume()
        {
            // only consume if successfully initialized
            return _initialized;
        }
    }
}
