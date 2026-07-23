namespace MRK.UI.Steps
{
    /// <summary>
    /// Adds the "Themes" and "Open logs" buttons plus a version footer to Anghami's user-nav
    /// dropdown and removes Anghami's dark-mode toggle. Each button is a clone of the live Settings
    /// <c>.action</c>, so it picks up Anghami's current markup instead of hardcoded HTML that goes
    /// stale each build.
    /// </summary>
    public class ThemesButtonStep : IUiStep
    {
        // the only theme-ish symbol in Anghami's icon sprite
        private const string ThemesIcon = "#all--darkmode";

        // Anghami's report-a-problem symbol, a good fit for opening the diagnostic log
        private const string LogsIcon = "#all--report-problem";

        public string EnsureFunctionName => "mrkEnsureThemesButton";

        public Task<string> BuildJavaScriptAsync()
        {
            // %%ICON%% / %%VERSION%% are swapped in after, so the brace-heavy JS stays a plain string
            var js = """
                function mrkEnsureThemesButton() {
                    if (document.getElementById('mrk-themes-btn')) return;

                    var nav = document.getElementsByTagName('anghami-user-navigation')[0];
                    if (!nav) return;

                    // anghamiUserNav -> container -> dropdown -> scrollbar -> options
                    var optionsContainer = nav.firstChild.children[1].lastChild.firstChild;
                    if (!optionsContainer) return;

                    var darkMode = optionsContainer.getElementsByClassName('action dark');
                    if (darkMode.length == 1) darkMode[0].remove();

                    // clone a real action so we inherit Anghami's current markup, not stale HTML
                    var template = optionsContainer.getElementsByClassName('action')[0];
                    if (!template) return;

                    var themes = template.cloneNode(true);
                    themes.id = 'mrk-themes-btn';
                    themes.classList.remove('dark');
                    var tLink = themes.querySelector('a');
                    if (tLink) {
                        tLink.removeAttribute('href');
                        tLink.removeAttribute('target');
                        tLink.removeAttribute('rel');
                        tLink.style.cursor = 'pointer';
                        tLink.onclick = function (e) {
                            if (e) e.preventDefault();
                            window.chrome.webview.postMessage('themes');
                            return false;
                        };
                    }
                    var tUse = themes.querySelector('use');
                    if (tUse) {
                        tUse.setAttribute('xlink:href', '%%ICON%%');
                        tUse.setAttribute('href', '%%ICON%%');
                    }
                    var tSvg = themes.querySelector('svg');
                    if (tSvg) tSvg.setAttribute('title', 'themes');
                    var tLabel = themes.querySelector('span');
                    if (tLabel) tLabel.textContent = 'Themes';
                    template.insertAdjacentElement('afterend', themes);

                    // logs button: same clone, opens the trace log folder via a 'logs' web message
                    var logs = template.cloneNode(true);
                    logs.id = 'mrk-logs-btn';
                    logs.classList.remove('dark');
                    var lLink = logs.querySelector('a');
                    if (lLink) {
                        lLink.removeAttribute('href');
                        lLink.removeAttribute('target');
                        lLink.removeAttribute('rel');
                        lLink.style.cursor = 'pointer';
                        lLink.onclick = function (e) {
                            if (e) e.preventDefault();
                            window.chrome.webview.postMessage('logs');
                            return false;
                        };
                    }
                    var lUse = logs.querySelector('use');
                    if (lUse) {
                        lUse.setAttribute('xlink:href', '%%LOGS_ICON%%');
                        lUse.setAttribute('href', '%%LOGS_ICON%%');
                    }
                    var lSvg = logs.querySelector('svg');
                    if (lSvg) lSvg.setAttribute('title', 'logs');
                    var lLabel = logs.querySelector('span');
                    if (lLabel) lLabel.textContent = 'Open logs';
                    themes.insertAdjacentElement('afterend', logs);

                    // version footer: same clone, icon stripped, muted label
                    var footer = template.cloneNode(true);
                    footer.classList.remove('dark');
                    footer.classList.add('mrk-version-footer');
                    var fLink = footer.querySelector('a');
                    if (fLink) {
                        fLink.removeAttribute('href');
                        fLink.removeAttribute('target');
                        fLink.removeAttribute('rel');
                    }
                    var fIcon = footer.querySelector('i');
                    if (fIcon) fIcon.remove();
                    var fLabel = footer.querySelector('span');
                    if (fLabel) fLabel.textContent = 'BetterAnghami v%%VERSION%%';
                    logs.insertAdjacentElement('afterend', footer);
                }
                """;

            js = js.Replace("%%ICON%%", ThemesIcon)
                .Replace("%%LOGS_ICON%%", LogsIcon)
                .Replace("%%VERSION%%", AppUtils.AppVersion);

            return Task.FromResult(js);
        }
    }
}
