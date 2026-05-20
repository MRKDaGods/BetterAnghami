using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    /// <summary>
    /// Injects custom CSS into the document's body
    /// <para>For now we only inject BetterAnghami.css</para>
    /// <para><b>DOMLoaded</b> action</para>
    /// </summary>
    public class InjectCustomCSSAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => false;

        public override async Task Execute()
        {
            // skip if already injected
            var exists = await WebView.ExecuteScriptAsync(
                "document.getElementById('mrk-better-css') != null"
            );
            if (exists == "true")
                return;

            string injectedCss = await AppUtils.ReadEmbeddedResource("CSS.BetterAnghami.css");

            // create <style> element with a stable id; inject in body to override inline body styling
            await WebView.ExecuteScriptAsync(
                $"""
                var style = document.createElement('style');
                style.id = 'mrk-better-css';
                style.type = 'text/css';
                style.innerHTML = `{injectedCss}`;

                document.body.appendChild(style);
                """
            );

            // Login page: replace promo h3 with BetterAnghami branding
            if (WebView.Source.StartsWith(Links.Login))
            {
                await WebView.ExecuteScriptAsync(
                    """
                    var h3 = document.querySelector('.main-login-body h3');
                    if (h3) h3.textContent = 'Log in to BetterAnghami';
                    """
                );
            }
        }

        public override bool ShouldConsume()
        {
            return false;
        }
    }
}
