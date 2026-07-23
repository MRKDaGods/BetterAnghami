using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using MRK.Actions;
using MRK.Models;
using MRK.UI;

namespace MRK
{
    /// <summary>
    /// Main Window
    /// </summary>
    public partial class AnghamiWindow : Window
    {
        private readonly ObjectReference<bool> _running;

        /// <summary>
        /// Serializes concurrent SourceChanged invocations so they don't race
        /// </summary>
        private readonly SemaphoreSlim _sourceChangedGate = new(1, 1);

        /// <summary>
        /// Anghami RPC instance
        /// </summary>
        private readonly AnghamiRPC _anghamiRPC;
        private readonly WebViewResizeFix _resizeFix;

        public SongService SongService { get; }

        public CoreWebView2 WebView => webViewControl.CoreWebView2;

        /// <summary>
        /// Is our app running?
        /// </summary>
        public bool IsRunning
        {
            get { return _running.Value; }
            set
            {
                lock (_running)
                {
                    _running.Value = value;
                }
            }
        }

#nullable disable
        public static AnghamiWindow Instance { get; private set; }

#nullable enable

        private static ActionManager ActionManager => ActionManager.Instance;

        public AnghamiWindow()
        {
            // assign instance
            Instance = this;

            // create running ref
            _running = new(true);

            SongService = new SongService(() => IsRunning);
            _anghamiRPC = new AnghamiRPC(SongService);
            _resizeFix = new WebViewResizeFix(this);

            InitializeComponent();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Tracer.Info(Tracer.Category.App, "Window loaded");

            // initialize themes. a bad themes file used to throw here and skip the webview entirely,
            // so a theme failure can't take the whole app down anymore
            try
            {
                await ThemeManager.Instance.LoadInstalledThemes();
            }
            catch (Exception ex)
            {
                Tracer.Error(Tracer.Category.Theme, "Failed to load installed themes", ex);
            }

            // initialize webview
            try
            {
                await InitializeWebView();
            }
            catch (Exception ex)
            {
                Tracer.Error(Tracer.Category.WebView, "WebView initialization failed", ex);
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _resizeFix.Detach();

            // close all other windows
            foreach (Window window in Application.Current.Windows)
            {
                if (window != this)
                {
                    window.Close();
                }
            }

            // clean up rpc if enabled
            _anghamiRPC.Stop();

            // set running false, for other threads to exit
            IsRunning = false;
        }

        /// <summary>
        /// Initializes CoreWebView2 and loads up Anghami Home
        /// </summary>
        private async Task InitializeWebView()
        {
            // initialize webview
            Tracer.Info(Tracer.Category.WebView, "Ensuring CoreWebView2");
            await webViewControl.EnsureCoreWebView2Async();
            Tracer.Info(
                Tracer.Category.WebView,
                $"CoreWebView2 ready, runtime {WebView.Environment.BrowserVersionString}"
            );

            _resizeFix.Attach();

            webViewControl.DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 9, 9, 11);

            // inject dark cover at document creation to block page content until loading screen takes over
            // observe `document` and insert a fixed overlay div as soon as <body> appears
            await WebView.AddScriptToExecuteOnDocumentCreatedAsync(
                """
                (function () {
                    var applied = false;
                    function injectCover() {
                        if (!applied && document.body) {
                            applied = true;
                            var d = document.createElement('div');
                            d.id = 'mrk-cover';
                            d.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:#09090b;pointer-events:none';
                            document.body.appendChild(d);
                        }
                    }
                    try {
                        new MutationObserver(injectCover).observe(document, { childList: true, subtree: true });
                    } catch (e) {
                        // fall back to readystate-driven retry if observe somehow fails
                        document.addEventListener('readystatechange', injectCover);
                    }
                    injectCover();
                })();
                """
            );

            // Self-healing UI: CSS, theme, Themes button, login branding (see BetterUiReconciler)
            await new BetterUiReconciler(WebView).InstallAsync();

            // attach event handlers
            WebView.DOMContentLoaded += OnWebViewDOMContentLoaded;
            WebView.SourceChanged += OnWebViewSourceChanged;
            WebView.ContextMenuRequested += OnWebViewContextMenuRequested;
            WebView.WebMessageReceived += OnWebViewMessageReceived;

            // settings
            WebView.Settings.IsWebMessageEnabled = true;
            WebView.Settings.IsStatusBarEnabled = false;

            // Boot on /home, not /login. Anghami only restores the last-played queue
            // (the /playqueue/fetch call that renders the bottom player bar) when the SPA
            // boots on an in-app route. Booting on /login and letting Anghami redirect to
            // /home skips that restore, so the player bar never shows until a real re-login.
            // Anghami does NOT bounce signed-out users off /home, so CheckLoginAction detects
            // the signed-out state (login button present) and redirects to /login itself.
            Tracer.Info(Tracer.Category.WebView, $"Navigating to {Links.Home}");
            WebView.Navigate(Links.Home);

            // register initial actions
            RegisterSourceChangedActions();
            RegisterDOMContentLoadedActions();
        }

        private void OnWebViewMessageReceived(
            object? sender,
            CoreWebView2WebMessageReceivedEventArgs e
        )
        {
            switch (e.WebMessageAsJson)
            {
                case "\"themes\"":
                    new ThemesWindow().Show();
                    break;

                case "\"logs\"":
                    OpenLogs();
                    break;
            }
        }

        /// <summary>
        /// Opens the current trace log in Notepad, falling back to the app data folder if the log
        /// file isn't there yet.
        /// </summary>
        private static void OpenLogs()
        {
            try
            {
                Tracer.Debug(Tracer.Category.Ui, "Opening logs from menu");

                var logPath = Tracer.LogFilePath;
                if (logPath != null && File.Exists(logPath))
                {
                    Process.Start("notepad.exe", $"\"{Path.GetFullPath(logPath)}\"");
                }
                else
                {
                    // no log yet, just open the folder
                    Process.Start(
                        new ProcessStartInfo(Path.GetFullPath(FileManager.Instance.BaseFolderPath))
                        {
                            UseShellExecute = true,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Tracer.Warn(Tracer.Category.Ui, "Failed to open logs", ex);
            }
        }

        /// <summary>
        /// DOMContentLoaded event handler
        /// </summary>
        private async void OnWebViewDOMContentLoaded(
            object? sender,
            CoreWebView2DOMContentLoadedEventArgs e
        )
        {
            await ActionManager.ExecuteActions(WebViewEvent.DOMLoaded);
        }

        /// <summary>
        /// SourceChanged event handler
        /// </summary>
        private async void OnWebViewSourceChanged(
            object? sender,
            CoreWebView2SourceChangedEventArgs e
        )
        {
            await _sourceChangedGate.WaitAsync();
            try
            {
                Tracer.Debug(Tracer.Category.WebView, $"SourceChanged {WebView.Source}");

                // execute all pre-load actions
                await ActionManager.ExecuteActions(
                    WebViewEvent.SourceChanged,
                    x => !x.WaitForLoad,
                    false
                );

                // wait for AnghamiBase to load
                await WaitForAnghamiLoad();

                // execute post-load actions
                await ActionManager.ExecuteActions(WebViewEvent.SourceChanged, x => x.WaitForLoad);
            }
            finally
            {
                _sourceChangedGate.Release();
            }
        }

        /// <summary>
        /// ContextMenuRequested event handler
        /// </summary>
        private void OnWebViewContextMenuRequested(
            object? sender,
            CoreWebView2ContextMenuRequestedEventArgs e
        )
        {
            // disable some context menu items
            for (int i = e.MenuItems.Count - 1; i >= 0; i--)
            {
                var menuItem = e.MenuItems[i];
                switch (menuItem.CommandId)
                {
                    case 33002: // reload
                    case 50101: // openLinkInNewWindow
                    case 50103: // saveLinkAs
                        e.MenuItems.RemoveAt(i);
                        break;
                }
            }
        }

        /// <summary>
        /// Polls for anghami-base in 500ms intervals. Bails immediately on the login page
        /// and gives up after 15 seconds.
        /// </summary>
        private async Task WaitForAnghamiLoad()
        {
            // login page never has anghami-base
            if (WebView.Source.StartsWith(Links.Login))
                return;

            const string findAnghamiBase = """document.body.innerHTML.indexOf("anghami-base")""";

            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline)
            {
                if (await ActionManager.ExecuteActionRaw(findAnghamiBase) != "-1")
                    return;

                await Task.Delay(500);
            }
        }

        /// <summary>
        /// Registers inital SourceChanged actions
        /// </summary>
        private void RegisterSourceChangedActions()
        {
            ActionManager.RegisterAction(WebViewEvent.SourceChanged, new CheckLoginAction(WebView));
            ActionManager.RegisterAction(
                WebViewEvent.SourceChanged,
                new ShowWelcomeAction(WebView)
            );
            ActionManager.RegisterAction(
                WebViewEvent.SourceChanged,
                new RemoveDesktopLinkAction(WebView)
            );
            ActionManager.RegisterAction(
                WebViewEvent.SourceChanged,
                new SyncWindowThemeAction(WebView)
            );
            ActionManager.RegisterAction(
                WebViewEvent.SourceChanged,
                new InitializeDiscordRPC(WebView, _anghamiRPC)
            );
        }

        /// <summary>
        /// Registers inital DOMContentLoaded actions
        /// </summary>
        private void RegisterDOMContentLoadedActions()
        {
            // CSS injection + login rebranding live in the reconciler now (see BetterUiReconciler),
            // so InjectCustomCSSAction is gone
            ActionManager.RegisterAction(
                WebViewEvent.DOMLoaded,
                new SetLoadingScreenAction(WebView)
            );
        }

        /// <summary>
        /// Immediately applies the provided theme properties to the document
        /// </summary>
        public async Task ApplyThemeImmediate(ThemePropertyList props)
        {
            Tracer.Info(Tracer.Category.Theme, "Applying theme to document");

            var cssVars = props.BuildCssPropertyList();

            // Set each variable as an inline style on <html> - inline specificity beats any stylesheet,
            // and individual setProperty calls survive Anghami's JS touching body.style
            await ActionManager.ExecuteActionRaw(
                $$"""
                (function() { {{cssVars}} })();
                """
            );

            // Clear the playing-label so ThemeStep recomputes it for the new theme, then reconcile
            await ActionManager.ExecuteActionRaw(
                """
                document.documentElement.style.removeProperty('--mrk-playing-label');
                window.__mrkReconcile && window.__mrkReconcile();
                """
            );

            // Sync the WPF window chrome to match
            SyncWindowTheme(props);
        }

        /// <summary>
        /// Applies the theme to the WPF window itself (title-bar border colour from --app-background).
        /// ThemeStep owns the web content; this is the part page JS can't reach.
        /// </summary>
        public void SyncWindowTheme(ThemePropertyList props)
        {
            var appBg = props.FirstOrDefault(x => x.Name == "--app-background");
            if (appBg == null)
            {
                Tracer.Warn(
                    Tracer.Category.Theme,
                    "Theme has no --app-background, skipping window sync"
                );
                return;
            }

            var color = ColorUtility.MatchColors(appBg.Value).FirstOrDefault()?.Color;
            if (color != null)
            {
                BorderBrush = new SolidColorBrush(color.Value);
            }
        }
    }
}
