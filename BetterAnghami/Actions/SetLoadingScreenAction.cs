using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    public class SetLoadingScreenAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        private bool _brandRevealed;

        /// <summary>
        /// True once the loading screen has finished its reveal (or there was none to show).
        /// This action never consumes itself (it keeps dropping the cover on later navigations),
        /// so callers wait on this instead of the action leaving the queue.
        /// <para>
        /// Fixes a hang in <see cref="ShowWelcomeAction"/>, which used to wait like this:
        /// </para>
        /// <code>
        /// do await Task.Delay(50);
        /// while (ActionManager.Instance.GetRunningAction&lt;SetLoadingScreenAction&gt;() != null);
        /// </code>
        /// </summary>
        public bool IsFinished => _brandRevealed;

        public override bool WaitForLoad => false;

        public override async Task Execute()
        {
            // On subsequent navigations the brand reveal has
            // just drop any leftover cover overlay and bail out
            if (_brandRevealed)
            {
                await WebView.ExecuteScriptAsync(
                    "var s=document.getElementById('mrk-cover');if(s)s.remove();"
                );
                return;
            }

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
                    "var s=document.getElementById('mrk-cover');if(s)s.remove();"
                );
                _brandRevealed = true;
                return;
            }

            // mutate the preloader in-place, then drop the cover
            await WebView.ExecuteScriptAsync(
                $$"""
                mrk_preloader.id = 'mrk-app-preloader-wrapper';
                mrk_preloader.style.setProperty('background-color', '#09090b');
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
                var s = document.getElementById('mrk-cover');
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

            _brandRevealed = true;
        }

        public override bool ShouldConsume()
        {
            return false;
        }
    }
}
