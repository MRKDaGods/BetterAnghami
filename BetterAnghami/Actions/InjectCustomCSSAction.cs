using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

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
            string injectedCss = await Utils.ReadEmbeddedResource("CSS.BetterAnghami.css");

            // create <style> element, but inject in body to override inline body styling
            await WebView.ExecuteScriptAsync($"""
                var style = document.createElement('style');
                style.type = 'text/css';
                style.innerHTML = `{injectedCss}`;

                document.body.appendChild(style);
                """);
        }

        public override bool ShouldConsume()
        {
            return true;
        }
    }
}
