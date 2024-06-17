using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace MRK.Actions
{
    /// <summary>
    /// Checks whether the user is currently logged in or not
    /// <para><b>SourceChanged</b> action</para>
    /// </summary>
    public class CheckLoginAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => true;

        public override async Task Execute()
        {
            // are we logged in?
            var result = await WebView.ExecuteScriptAsync("""
                document.getElementsByClassName("anghami-primary-btn login-btn a-like").length
                """);

            if (result == "1")
            {
                // we are not logged in, go to login
                WebView.Navigate(Links.Login);
            }
        }

        public override bool ShouldConsume()
        {
            return false;
        }
    }
}
