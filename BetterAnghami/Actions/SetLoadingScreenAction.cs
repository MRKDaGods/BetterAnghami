using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    public class SetLoadingScreenAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => false;

        public override async Task Execute()
        {
            // check if preloader wrapper is present
            var preloaderExists = await WebView.ExecuteScriptAsync(
                """
                var mrk_preloader = document.getElementById("app-preloader-wrapper");
                mrk_preloader != null; // return this
                """
            );

            if (preloaderExists != "true")
            {
                // no preloader, drop the cover
                await WebView.ExecuteScriptAsync(
                    "var s=document.getElementById('mrk-cover-style');if(s)s.remove();"
                );
                return;
            }

            // mutate in-place to avoid a one-frame DOM gap, then drop the cover
            await WebView.ExecuteScriptAsync(
                $$"""
                mrk_preloader.id = 'mrk-app-preloader-wrapper';
                mrk_preloader.innerHTML = `
                    <div class="mrk-brand">
                        <img src="https://cdnweb.anghami.com/web/assets/img/logos/New_Logo_Dark@2x.png" alt="anghami" width="96">
                        <span class="mrk-better-text">BETTER</span>
                    </div>
                    <div class="mrk-dots">
                        <span></span><span></span><span></span>
                    </div>
                    <span class="mrk-version">v{{AppUtils.AppVersion}}</span>
                `;
                var s = document.getElementById('mrk-cover-style');
                if (s) s.remove();
                """
            );

            // wait for a bit
            // anghami loading is laggy
            await Task.Delay(1500);

            // show BETTER label
            await WebView.ExecuteScriptAsync(
                """
                var betterText = mrk_preloader.querySelector('.mrk-better-text');
                betterText.style.setProperty('width', '265px');
                betterText.style.setProperty('margin-left', '26px');
                betterText.style.setProperty('opacity', '1');
                """
            );

            await Task.Delay(1500);

            // fade out loading
            await WebView.ExecuteScriptAsync(
                """
                mrk_preloader.style.setProperty("opacity", "0");
                """
            );

            await Task.Delay(1000);

            // remove preloader
            await WebView.ExecuteScriptAsync(
                """
                mrk_preloader.remove();
                """
            );
        }

        public override bool ShouldConsume()
        {
            return true;
        }
    }
}
