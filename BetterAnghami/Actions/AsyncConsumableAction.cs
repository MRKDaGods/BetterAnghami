using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace MRK.Actions
{
    /// <summary>
    /// Base class for all asynchronous consumable actions
    /// </summary>
    public class AsyncConsumableAction
    {
        protected CoreWebView2 WebView { get; init; }

        /// <summary>
        /// Should the action wait for Anghami to load? (Check for AnghamiBase)
        /// </summary>
        public virtual bool WaitForLoad => false;
        
        /// <summary>
        /// Delay before executing the action
        /// </summary>
        public virtual int ExecutionDelay => 0;

        public AsyncConsumableAction(CoreWebView2 webView)
        {
            WebView = webView;
        }

        /// <summary>
        /// Action async handler
        /// </summary>
        public virtual Task Execute()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Should the action get consumed and removed from the action queue?
        /// </summary>
        public virtual bool ShouldConsume()
        {
            return false;
        }
    }
}
