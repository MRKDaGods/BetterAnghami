using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace MRK.Actions
{
    public class SetCustomThemeAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => false;

        public override async Task Execute()
        {
            await WebView.ExecuteScriptAsync(Scripts.SetCustomTheme());
        }
        
        public override bool ShouldConsume()
        {
            return true;
        }
    }
}
