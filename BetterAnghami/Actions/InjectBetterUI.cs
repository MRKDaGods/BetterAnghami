using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace MRK.Actions
{
    /// <summary>
    /// Injects custom BetterAnghami UI into the webpage
    /// <para><b>SourceChanged</b> action</para>
    /// </summary>
    public class InjectBetterUI(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        /// <summary>
        /// Did we inject the main UI? Themes button in hamburger menu, etc
        /// </summary>
        private bool _hasInjectedMainUI;

        public override bool WaitForLoad => true;
        public override int ExecutionDelay => 1000; // wait for a bit post-load

        public override async Task Execute()
        {
            if (!_hasInjectedMainUI)
            {
                var themesButton = await Utils.ReadEmbeddedResource("HTML.ThemesButton.html");
                // inject themes button
                _hasInjectedMainUI = await WebView.ExecuteScriptAsync($$"""
                (function() {
                    try {
                        // get options container, excluding the pfp container
                        // anghamiUserNav -> container -> dropdown container -> scrollbar container -> options container
                        var optionsContainer = document.getElementsByTagName("anghami-user-navigation")[0].firstChild.children[1].lastChild.firstChild;
                        
                        // remove Dark mode
                        var darkMode = optionsContainer.getElementsByClassName("action dark");
                        if (darkMode.length == 1) {
                            darkMode[0].remove();
                        }

                        // use settings button as a reference
                        var settingsButton = optionsContainer.getElementsByClassName("action")[0];

                        // insert themes after settings
                        settingsButton.insertAdjacentHTML("afterend", `{{themesButton}}`);

                        return true;
                    }
                    catch (e) {
                        console.log(e);
                        return false;
                    }
                })()
                """) == "true";
            }
        }

        public override bool ShouldConsume()
        { 
            return false;
        }
    }
}
