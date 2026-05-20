using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    /// <summary>
    /// Injects custom BetterAnghami UI into the webpage
    /// <para><b>SourceChanged</b> action</para>
    /// </summary>
    public class InjectBetterUI(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => true;
        public override int ExecutionDelay => 1000; // wait for a bit post-load

        public override async Task Execute()
        {
            // check DOM presence
            var alreadyInjected = await WebView.ExecuteScriptAsync(
                "document.getElementById('mrk-themes-btn') != null"
            );
            if (alreadyInjected == "true")
                return;

            var themesButton = await AppUtils.ReadEmbeddedResource("HTML.ThemesButton.html");
            await WebView.ExecuteScriptAsync(
                $$"""
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

                        // insert version footer after themes button
                        settingsButton.nextElementSibling.insertAdjacentHTML("afterend", `<li _ngcontent-anghami-web-v2-c99="" class="action mrk-version-footer"><a _ngcontent-anghami-web-v2-c99=""><span _ngcontent-anghami-web-v2-c99="">BetterAnghami v{{AppUtils.AppVersion}}</span></a></li>`);
                    }
                    catch (e) {
                        console.log(e);
                    }
                })()
                """
            );
        }

        public override bool ShouldConsume()
        {
            return false;
        }
    }
}
