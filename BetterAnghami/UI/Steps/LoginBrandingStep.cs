namespace MRK.UI.Steps
{
    /// <summary>
    /// Rebrands the login page: replaces Anghami's promo heading with "Log in to BetterAnghami".
    /// Runs only on the login route and re-applies if Anghami re-renders the heading.
    /// </summary>
    public class LoginBrandingStep : IUiStep
    {
        public string EnsureFunctionName => "mrkEnsureLoginBranding";

        public Task<string> BuildJavaScriptAsync()
        {
            return Task.FromResult(
                """
                function mrkEnsureLoginBranding() {
                    if (location.pathname.indexOf('login') === -1) return;
                    var h3 = document.querySelector('.main-login-body h3');
                    if (h3 && h3.textContent !== 'Log in to BetterAnghami') {
                        h3.textContent = 'Log in to BetterAnghami';
                    }
                }
                """
            );
        }
    }
}
