using System.Text;
using Microsoft.Web.WebView2.Core;
using MRK.UI.Steps;

namespace MRK.UI
{
    /// <summary>
    /// Installs the self-healing UI: one document-created script with one debounced
    /// <c>MutationObserver</c> that re-runs every <see cref="IUiStep"/> whenever the DOM changes, so
    /// the CSS, theme, Themes button and login branding come back after Anghami re-renders (the
    /// post-login home rebuild used to drop them). One-shots stay as normal <see cref="Actions"/>.
    /// </summary>
    public class BetterUiReconciler
    {
        private readonly CoreWebView2 _webView;

        // Order matters: CSS first so everything else is styled, theme before the button
        private readonly List<IUiStep> _steps =
        [
            new CssStep(),
            new ThemeStep(),
            new ThemesButtonStep(),
            new LoginBrandingStep(),
        ];

        public BetterUiReconciler(CoreWebView2 webView)
        {
            _webView = webView;
        }

        /// <summary>
        /// Builds the composed reconciler script from the registered steps and installs it to run on
        /// every document creation.
        /// </summary>
        public async Task InstallAsync()
        {
            var stepJs = new StringBuilder();
            var stepNames = new List<string>();

            foreach (var step in _steps)
            {
                // one step failing to build shouldn't drop every other step
                try
                {
                    stepJs.AppendLine(await step.BuildJavaScriptAsync());
                    stepJs.AppendLine();
                    stepNames.Add(step.EnsureFunctionName);
                }
                catch (Exception ex)
                {
                    Tracer.Error(
                        Tracer.Category.Ui,
                        $"Step {step.GetType().Name} failed to build",
                        ex
                    );
                }
            }

            // Names first, so the step JS can't collide with the token
            var script = ReconcilerTemplate
                .Replace("%%STEP_NAMES%%", string.Join(", ", stepNames))
                .Replace("%%STEPS%%", stepJs.ToString());

            await _webView.AddScriptToExecuteOnDocumentCreatedAsync(script);

            Tracer.Info(
                Tracer.Category.Ui,
                $"Reconciler installed with steps: {string.Join(", ", stepNames)}"
            );
        }

        // Step JS goes in at %%STEPS%%, the ordered ensure-fn list at %%STEP_NAMES%%
        private const string ReconcilerTemplate = """
            (function () {
                if (window.__mrkUiInstalled) return;
                window.__mrkUiInstalled = true;

                %%STEPS%%

                var STEPS = [%%STEP_NAMES%%];
                function reconcile() {
                    for (var i = 0; i < STEPS.length; i++) {
                        try { STEPS[i](); } catch (e) {}
                    }
                }

                // ApplyThemeImmediate calls this after a theme switch
                window.__mrkReconcile = reconcile;

                var scheduled = false;
                function schedule() {
                    if (scheduled) return;
                    scheduled = true;
                    setTimeout(function () { scheduled = false; reconcile(); }, 50);
                }

                // Observe `document`, not documentElement
                // Anghami can swap documentElement out, which kills an observer bound to it (that was the cold-boot CSS drop)
                try {
                    new MutationObserver(schedule).observe(document, { childList: true, subtree: true });
                } catch (e) {}

                reconcile();
                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', reconcile, { once: true });
                }
            })();
            """;
    }
}
