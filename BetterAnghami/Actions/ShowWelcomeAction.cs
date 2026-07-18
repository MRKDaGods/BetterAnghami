using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    /// <summary>
    /// Shows the welcome/changelog popup when the app version changes.
    /// <para><b>SourceChanged</b> action</para>
    /// </summary>
    public class ShowWelcomeAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => true;

        public override async Task Execute()
        {
            var config = Configuration.Instance;
            var lastVersion = config[Configuration.Keys.LastSeenVersion].String;
            var currentVersion = AppUtils.AppVersion;

            if (lastVersion == currentVersion)
                return;

            // Wait for the loading screen to finish its reveal. It never consumes itself (it keeps
            // dropping the cover on later navigations), so poll its state, not its queue presence.
            // The timeout is a safety net so a stalled/failed reveal can't hang here indefinitely.
            var loadingScreen = ActionManager.Instance.GetRunningAction<SetLoadingScreenAction>();
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (loadingScreen is { IsFinished: false } && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            bool isFirstLaunch = lastVersion.Length == 0;

            config[Configuration.Keys.LastSeenVersion] = currentVersion;

            bool isLoggedIn = !WebView.Source.StartsWith(Links.Login);

            Application.Current.Dispatcher.Invoke(() =>
                new WelcomeWindow(isLoggedIn, isFirstLaunch).Show()
            );
        }

        public override bool ShouldConsume() => true;
    }
}
