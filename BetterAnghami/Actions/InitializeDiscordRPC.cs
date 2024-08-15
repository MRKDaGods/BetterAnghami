using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace MRK.Actions
{
    /// <summary>
    /// Ensures initialization of Discord RPC, and starts the RPC thread
    /// </summary>
    public class InitializeDiscordRPC(CoreWebView2 webView, AnghamiRPC rpc) : AsyncConsumableAction(webView)
    {
        /// <summary>
        /// Has the RPC client been initialized?
        /// </summary>
        private bool _initialized = false;

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
                    // start thread
                    _anghamiRpc.StartRpcThread();
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
