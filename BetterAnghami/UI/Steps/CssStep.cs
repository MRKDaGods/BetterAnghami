namespace MRK.UI.Steps
{
    /// <summary>
    /// Keeps BetterAnghami.css injected as a single style#mrk-better-css. Anghami's SPA can drop it
    /// on a re-render, so this re-adds it when it's gone.
    /// </summary>
    public class CssStep : IUiStep
    {
        public string EnsureFunctionName => "mrkEnsureCss";

        public async Task<string> BuildJavaScriptAsync()
        {
            var css = await AppUtils.ReadEmbeddedResource("CSS.BetterAnghami.css");
            var cssLiteral = JsUtils.EscapeTemplateLiteral(css);

            // Mount in <head>/<body> (a stable node), not a bare documentElement child that a
            // bootstrap swap would lose. isConnected catches a stale detached node.
            return $$"""
                var MRK_CSS = `{{cssLiteral}}`;
                function mrkEnsureCss() {
                    var mount = document.head || document.body;
                    if (!mount) return;
                    var existing = document.getElementById('mrk-better-css');
                    if (existing && existing.isConnected) return;
                    if (existing) existing.remove();
                    var style = document.createElement('style');
                    style.id = 'mrk-better-css';
                    style.textContent = MRK_CSS;
                    mount.appendChild(style);
                }
                """;
        }
    }
}
