using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

namespace MRK.Actions
{
    /// <summary>
    /// Initially applies the user selected theme
    /// </summary>
    public class SetSelectedThemeAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => false;

        public override async Task Execute()
        {
            // get currently selected theme properties
            var themeManager = ThemeManager.Instance;

            var props = await themeManager.LoadTheme(themeManager.SelectedTheme);
            if (props == null)
            {
                throw new Exception("Cannot load selected theme props");
            }

            await AnghamiWindow.Instance.ApplyThemeImmediate(props);
        }

        public override bool ShouldConsume()
        {
            return true;
        }
    }
}
