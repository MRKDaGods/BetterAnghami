namespace MRK.UI.Steps
{
    /// <summary>
    /// Applies the selected theme's CSS vars to <c>&lt;html&gt;</c> and derives
    /// <c>--mrk-playing-label</c> for the currently-playing song title. Re-applies the theme only
    /// when <c>--app-background</c> got cleared (so it won't fight a runtime switch) and recomputes
    /// the label only when it's missing (keeps the reconcile loop cheap).
    /// <see cref="AnghamiWindow.ApplyThemeImmediate"/> forces a recompute on a runtime switch by
    /// clearing <c>--mrk-playing-label</c> then calling the reconciler.
    /// </summary>
    public class ThemeStep : IUiStep
    {
        public string EnsureFunctionName => "mrkEnsureTheme";

        public async Task<string> BuildJavaScriptAsync()
        {
            // one setProperty per var. values can be hex/rgb/hsl/var(), so we leave them as-is and
            // let the JS below resolve them (the browser handles every format).
            var selected = ThemeManager.Instance.SelectedTheme;
            var props = await ThemeManager.Instance.LoadTheme(selected);
            if (props == null)
            {
                Tracer.Warn(
                    Tracer.Category.Theme,
                    $"Selected theme '{selected.Name}' produced no properties, page stays unthemed"
                );
            }
            else
            {
                Tracer.Info(Tracer.Category.Theme, $"Baking theme '{selected.Name}' into reconciler");
            }

            var applyVarsBody =
                props == null
                    ? ""
                    : string.Join(
                        " ",
                        props.Select(p =>
                            $"s.setProperty('{p.Name}','{JsUtils.EscapeSingleQuoted(p.Value)}');"
                        )
                    );

            // %%THEME%% is swapped in after, so the brace-heavy JS stays a plain (non-interpolated) string
            var js = """
                function mrkThemeApplyVars() {
                    try { var s = document.documentElement.style; %%THEME%% } catch (e) {}
                }

                // resolve a CSS var to rgb via the browser (handles hex/rgb/hsl/var). the probe is
                // added and removed right away; since this only runs when the label var is missing,
                // that blip can't keep waking the observer.
                function mrkThemeResolveColor(name) {
                    var probe = document.createElement('span');
                    probe.style.cssText = 'position:absolute;opacity:0;pointer-events:none;color:var(' + name + ')';
                    document.documentElement.appendChild(probe);
                    var c = getComputedStyle(probe).color;
                    document.documentElement.removeChild(probe);
                    var m = c.match(/[\d.]+/g);
                    return (m && m.length >= 3) ? { r: +m[0], g: +m[1], b: +m[2] } : null;
                }

                function mrkThemeLum(c) { return (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255; }

                // blend two rgb colours (t=0 -> a, t=1 -> b)
                function mrkThemeMix(a, b, t) {
                    return {
                        r: Math.round(a.r + (b.r - a.r) * t),
                        g: Math.round(a.g + (b.g - a.g) * t),
                        b: Math.round(a.b + (b.b - a.b) * t)
                    };
                }

                // the playing row's title uses .purple-label (var(--brand-purple)), and some themes
                // set brand-purple ~ the background, so it's unreadable
                function mrkThemeUpdatePlayingLabel() {
                    var bg = mrkThemeResolveColor('--app-background');
                    var purple = mrkThemeResolveColor('--brand-purple');
                    if (!bg || !purple) return;

                    var label;
                    if (Math.abs(mrkThemeLum(bg) - mrkThemeLum(purple)) > 0.45) {
                        // readable against the bg, use it
                        label = purple;
                    } else {
                        // plain --text-color would make the playing row look like every other song,
                        // so tint it toward the theme's light accent instead. 0.4 = how far to nudge.
                        var text = mrkThemeResolveColor('--text-color');
                        var accent = mrkThemeResolveColor('--brand-purple-light');
                        label = (text && accent)
                            ? mrkThemeMix(text, accent, 0.4)
                            : (text || { r: 255 - purple.r, g: 255 - purple.g, b: 255 - purple.b });
                    }

                    document.documentElement.style.setProperty(
                        '--mrk-playing-label', 'rgb(' + label.r + ',' + label.g + ',' + label.b + ')'
                    );
                }

                function mrkEnsureTheme() {
                    var s = document.documentElement.style;
                    if (!s.getPropertyValue('--app-background')) {
                        mrkThemeApplyVars();
                        mrkThemeUpdatePlayingLabel();
                    } else if (!s.getPropertyValue('--mrk-playing-label')) {
                        mrkThemeUpdatePlayingLabel();
                    }
                }
                """;

            return js.Replace("%%THEME%%", applyVarsBody);
        }
    }
}
