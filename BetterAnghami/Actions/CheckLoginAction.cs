using Microsoft.Web.WebView2.Core;

namespace MRK.Actions
{
    /// <summary>
    /// Redirects to the login page when the user isn't signed in.
    /// <para>
    /// The app boots on /home so Anghami restores the player/queue, but Anghami does not
    /// bounce signed-out users off /home on its own. The header renders a beat after the
    /// page loads, so we poll until the auth state is unambiguous instead of checking once
    /// (the old single check ran too early, saw no login button yet, and gave up).
    /// </para>
    /// <para><b>SourceChanged</b> action</para>
    /// </summary>
    public class CheckLoginAction(CoreWebView2 webView) : AsyncConsumableAction(webView)
    {
        public override bool WaitForLoad => true;

        public override async Task Execute()
        {
            // Nothing to do if we're already on the login page
            if (WebView.Source.StartsWith(Links.Login))
                return;

            // Poll until we can tell the auth state
            //   signed IN  -> localStorage 'user' is set within ~0.5s; login button never appears
            //   signed OUT -> the login button is present and persists; 'user' is never set
            var deadline = DateTime.UtcNow.AddSeconds(12);
            while (DateTime.UtcNow < deadline)
            {
                // A navigation may have already carried us onto the login page
                if (WebView.Source.StartsWith(Links.Login))
                    return;

                var state = await WebView.ExecuteScriptAsync(
                    """
                    (function () {
                        if (localStorage.getItem('user')) return 'in';
                        if (document.querySelector('.login-btn')) return 'out';
                        return 'wait';
                    })()
                    """
                );

                if (state == "\"in\"")
                    return; // signed in, dont do anth

                if (state == "\"out\"")
                {
                    WebView.Navigate(Links.Login);
                    return;
                }

                // Header not settled yet; check again shortly
                await Task.Delay(300);
            }
        }

        public override bool ShouldConsume()
        {
            return false;
        }
    }
}
