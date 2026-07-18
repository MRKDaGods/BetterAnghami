using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    /// <summary>
    /// Syncs the WPF window chrome (title-bar border) to the selected theme once at startup.
    /// The page theme is owned by the reconciler's ThemeStep; this only covers what page JS can't
    /// touch (the WPF window itself). Runtime theme switches update the window via ApplyThemeImmediate.
    /// </summary>
    public class SyncWindowThemeAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        private bool _applied;

        public override bool WaitForLoad => false;

        public override async Task Execute()
        {
            var themeManager = ThemeManager.Instance;

            var props = await themeManager.LoadTheme(themeManager.SelectedTheme);
            if (props == null)
            {
                throw new Exception("Cannot load selected theme props");
            }

            AnghamiWindow.Instance.SyncWindowTheme(props);
            _applied = true;
        }

        // Window chrome only needs the selected theme applied once; runtime switches go through
        // ApplyThemeImmediate, so consume after the first successful apply
        public override bool ShouldConsume() => _applied;
    }
}
