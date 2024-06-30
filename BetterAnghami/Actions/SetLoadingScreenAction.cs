using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.Windows;

namespace MRK.Actions
{
    public class SetLoadingScreenAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => false;

        public override async Task Execute()
        {
            // check if preloader wrapper is present
            var preloaderExists = await WebView.ExecuteScriptAsync("""
                var mrk_preloader = document.getElementById("app-preloader-wrapper");
                mrk_preloader != null; // return this
                """);

            if (preloaderExists != "true")
            {
                MessageBox.Show("not found");
                return;
            }

            // start preloader animation
            await WebView.ExecuteScriptAsync("""
                mrk_preloader.outerHTML = `
                        <div id="mrk-app-preloader-wrapper">             
                             <!-- Anghami logo -->
                             <img src="https://cdnweb.anghami.com/web/assets/img/logos/New_Logo_Dark@2x.png" alt="preloader-logo" width="110">

                             <!-- Better anghami -->
                             <span>BETTER</span>
                        </div>
                `;
                
                mrk_preloader = document.getElementById("mrk-app-preloader-wrapper");
                """);

            // wait for a bit
            // anghami loading is laggy
            await Task.Delay(1500);

            // show BETTER label
            await WebView.ExecuteScriptAsync("""
                mrk_preloader.children[1].style.setProperty("width", "290px");
                mrk_preloader.children[1].style.setProperty("margin-left", "48px");
                """);

            await Task.Delay(1500);

            // fade out loading
            await WebView.ExecuteScriptAsync("""
                mrk_preloader.style.setProperty("opacity", "0");
                """);

            await Task.Delay(1000);

            // remove preloader
            await WebView.ExecuteScriptAsync("""
                mrk_preloader.remove();
                """);
        }

        public override bool ShouldConsume()
        {
            return true;
        }
    }
}
