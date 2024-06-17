using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace MRK.Actions
{
    /// <summary>
    /// Removes the InstallApp button in the navbar
    /// <para><b>SourceChanged</b> action</para>
    /// </summary>
    public class RemoveDesktopLinkAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        /// <summary>
        /// Has the link been removed? If so consume action
        /// </summary>
        private bool _hasRemovedLink;

        public override bool WaitForLoad => true;
        public override int ExecutionDelay => 1000; // wait for a bit post-load, navbar takes some time to show

        public override async Task Execute()
        {
            // remove the desktop link element
            _hasRemovedLink = await WebView.ExecuteScriptAsync("""
                var mrk_desktopLink = document.getElementsByClassName("desktop-link");
                var mrk_tempResult = mrk_desktopLink.length > 0;
                if (mrk_tempResult) {
                    mrk_desktopLink[0].remove();
                }

                mrk_tempResult;
                """) == "true";
        }

        public override bool ShouldConsume()
        {
            // navbar supposedly maintains its state
            // remove only 
            return _hasRemovedLink;
        }
    }
}
