using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using MRK.Actions;
using MRK.Models;

namespace MRK
{
    /// <summary>
    /// Main Window
    /// </summary>
    public partial class AnghamiWindow : Window, ISongHost
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

        /// <summary>
        /// Serializer options for Song JSON
        /// </summary>
        private readonly JsonSerializerOptions _songJsonSerializerOptions;

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

            // instantiate rpc singleton
            _anghamiRPC = new AnghamiRPC(this);

            // json options
            _songJsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            InitializeComponent();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // initialize themes
            await ThemeManager.Instance.LoadInstalledThemes();

            // initialize webview
            await InitializeWebView();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
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
            await webViewControl.EnsureCoreWebView2Async();

            webViewControl.DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 9, 9, 11);

            // inject dark cover at document creation to block page content until loading screen takes over
            await WebView.AddScriptToExecuteOnDocumentCreatedAsync(
                """
                (function () {
                    var s = document.createElement('style');
                    s.id = 'mrk-cover-style';
                    s.textContent = 'html::before{content:"";position:fixed!important;inset:0!important;z-index:2147483647!important;background:#09090b!important;pointer-events:none}';
                    document.documentElement.appendChild(s);
                    setTimeout(function () {
                        var el = document.getElementById('mrk-cover-style');
                        if (el) el.remove();
                    }, 8000);
                })();
                """
            );

            // attach event handlers
            WebView.DOMContentLoaded += OnWebViewDOMContentLoaded;
            WebView.SourceChanged += OnWebViewSourceChanged;
            WebView.ContextMenuRequested += OnWebViewContextMenuRequested;
            WebView.WebMessageReceived += OnWebViewMessageReceived;

            // settings
            WebView.Settings.IsWebMessageEnabled = true;
            WebView.Settings.IsStatusBarEnabled = false;

            // go to anghami home
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
            // for now we only have themes
            if (e.WebMessageAsJson == "\"themes\"")
            {
                // show themes window and wait for it to close
                new ThemesWindow().Show();
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
        /// Checks for AnghamiBase in 500ms intervals
        /// </summary>
        private static async Task WaitForAnghamiLoad()
        {
            const string findAnghamiBase = """document.body.innerHTML.indexOf("anghami-base")""";

            while (await ActionManager.ExecuteActionRaw(findAnghamiBase) == "-1")
            {
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
                new RemoveDesktopLinkAction(WebView)
            );
            ActionManager.RegisterAction(
                WebViewEvent.SourceChanged,
                new SetSelectedThemeAction(WebView)
            );
            ActionManager.RegisterAction(WebViewEvent.SourceChanged, new InjectBetterUI(WebView));
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
            ActionManager.RegisterAction(
                WebViewEvent.DOMLoaded,
                new InjectCustomCSSAction(WebView)
            );
            ActionManager.RegisterAction(
                WebViewEvent.DOMLoaded,
                new SetLoadingScreenAction(WebView)
            );
        }

        /// <summary>
        /// Gets the local Anghami user
        /// </summary>
        public async Task<User> GetLocalUser()
        {
            var json = await ActionManager.ExecuteActionRaw(
                """
                (function() {
                    var viewProfile = document.getElementsByClassName("viewprofile")[0];
                    var profileUrl = viewProfile.href;
                    var id = parseInt(profileUrl.substring(profileUrl.lastIndexOf('/') + 1));

                    // name is located in viewProfile's top sibling's text
                    var name = viewProfile.parentElement.firstChild.innerText;

                    return { Id: id, Name: name };
                })()
                """
            );

            if (json == "null")
            {
                throw new Exception("Cannot get local user");
            }

            return JsonSerializer.Deserialize<User>(json)!;
        }

        /// <summary>
        /// Immediately applies the provided theme properties to the document
        /// </summary>
        public async Task ApplyThemeImmediate(List<ThemeProperty> props)
        {
            var cssVars = string.Join(
                ' ',
                props.Select(x =>
                    $"document.documentElement.style.setProperty('{x.Name}','{x.Value}');"
                )
            );

            // set each variable as an inline style on <html> - inline specificity beats any stylesheet,
            // and individual setProperty calls survive Anghami's JS touching body.style
            await ActionManager.ExecuteActionRaw(
                $$"""
                (function() { {{cssVars}} })();
                """
            );

            // update window title bar colour
            var appBg = props.Find(x => x.Name == "--app-background");
            if (appBg != null)
            {
                var color = ColorUtility.MatchColors(appBg.Value).FirstOrDefault()?.Color;
                if (color != null)
                {
                    BorderBrush = new SolidColorBrush(color.Value);
                }
            }
        }

        /// <summary>
        /// Gets the currently playing song regardless of playing state
        /// </summary>
        public async Task<Song?> GetCurrentlyPlayingSong()
        {
            if (!IsRunning)
            {
                return null;
            }

            var json = await ActionManager.ExecuteActionRaw(
                """
                (function() {
                    // too lazy to use getxxx
                    const infoContainer = document.querySelector(".image-info-container");

                    // get image url
                    const bgImage = infoContainer.querySelector(".track-coverart").style.backgroundImage;
                    const imgUrlStart = bgImage.indexOf('"') + 1;
                    const imgUrlEnd = bgImage.lastIndexOf('"');
                    const imgUrl = bgImage.substring(imgUrlStart, imgUrlEnd);
                    
                    // get song name and id
                    const titleAnchor = infoContainer.querySelector(".action-title");
                    const name = titleAnchor.innerText;
                    const id = parseInt(titleAnchor.href.substring(titleAnchor.href.lastIndexOf('/') + 1)) || -1; // local files have no id

                    // get artist
                    const artistAnchor = infoContainer.querySelector(".action-artist");
                    const artist = artistAnchor.innerText;

                    // play details
                    const mainPlayer = document.querySelector(".main-player");
                    const playPauseCont = mainPlayer.querySelector(".play-pause-cont");
                    const playState = playPauseCont.children[0].classList[1]; // button name is the second class as of 12/7/2024

                    // durations
                    const durations = mainPlayer.querySelectorAll(".duration-text");
                    let durStart = "--", durEnd = "--";
                    if (durations.length == 2) {
                        durStart = durations[0].innerText;
                        durEnd = durations[1].innerText; // remaining time
                    }

                    return {
                        id,
                        name,
                        artist,
                        imgUrl,
                        playState,
                        durStart,
                        durEnd
                    };
                })()
                """
            );

            // dont attempt to convert if un-necessary
            if (json == "null")
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Song>(json, _songJsonSerializerOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the currently playing song synchronously
        /// </summary>
        Song? ISongHost.GetCurrentlyPlayingSong()
        {
            return Dispatcher.Invoke(GetCurrentlyPlayingSong).GetAwaiter().GetResult();
        }
    }
}
