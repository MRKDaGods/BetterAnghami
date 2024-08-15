using Microsoft.Web.WebView2.Core;
using MRK.Actions;
using MRK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MRK
{
    /// <summary>
    /// Main Window
    /// </summary>
    public partial class AnghamiWindow : Window, ISongHost
    {
        private readonly ObjectReference<bool> _running;

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
            get
            {
                return _running.Value;
            }

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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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

        private void OnWebViewMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
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
        private async void OnWebViewDOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            await ActionManager.ExecuteActions(WebViewEvent.DOMLoaded);
        }

        /// <summary>
        /// SourceChanged event handler
        /// </summary>
        private async void OnWebViewSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            // execute all pre-load actions
            // dont remove actions yet
            await ActionManager.ExecuteActions(WebViewEvent.SourceChanged,
                x => !x.WaitForLoad,
                false);

            // wait for AnghamiBase to load
            await WaitForAnghamiLoad();

            // execute post-load actions
            await ActionManager.ExecuteActions(WebViewEvent.SourceChanged,
                x => x.WaitForLoad);
        }

        /// <summary>
        /// ContextMenuRequested event handler
        /// </summary>
        private void OnWebViewContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            // disable some context menu items
            for (int i = e.MenuItems.Count - 1; i >= 0; i--)
            {
                var menuItem = e.MenuItems[i];
                switch (menuItem.CommandId)
                {
                    case 33002:     // reload
                    case 50101:     // openLinkInNewWindow
                    case 50103:     // saveLinkAs
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
            ActionManager.RegisterAction(WebViewEvent.SourceChanged, new RemoveDesktopLinkAction(WebView));
            ActionManager.RegisterAction(WebViewEvent.SourceChanged, new SetSelectedThemeAction(WebView));
            ActionManager.RegisterAction(WebViewEvent.SourceChanged, new InjectBetterUI(WebView));
            ActionManager.RegisterAction(WebViewEvent.SourceChanged, new InitializeDiscordRPC(WebView, _anghamiRPC));
        }

        /// <summary>
        /// Registers inital DOMContentLoaded actions
        /// </summary>
        private void RegisterDOMContentLoadedActions()
        {
            ActionManager.RegisterAction(WebViewEvent.DOMLoaded, new InjectCustomCSSAction(WebView));
            ActionManager.RegisterAction(WebViewEvent.DOMLoaded, new SetLoadingScreenAction(WebView));
        }

        /// <summary>
        /// Gets the local Anghami user
        /// </summary>
        public async Task<User> GetLocalUser()
        {
            var json = await ActionManager.ExecuteActionRaw("""
                (function() {
                    var viewProfile = document.getElementsByClassName("viewprofile")[0];
                    var profileUrl = viewProfile.href;
                    var id = parseInt(profileUrl.substring(profileUrl.lastIndexOf('/') + 1));

                    // name is located in viewProfile's top sibling's text
                    var name = viewProfile.parentElement.firstChild.innerText;

                    return { Id: id, Name: name };
                })()
                """);

            if (json == "null")
            {
                throw new Exception("Cannot get local user");
            }

            return JsonSerializer.Deserialize<User>(json)!;
        }

        /// <summary>
        /// Immediately applies the provided theme properties to the document body
        /// </summary>
        public async Task ApplyThemeImmediate(List<ThemeProperty> props)
        {
            var inlineCss = string.Join('\n',
                props.Select(x => $"{x.Name}: {x.Value};"));

            await ActionManager.ExecuteActionRaw($"""
                document.body.style.cssText = `{inlineCss}`;
                """);

            // update window title bar
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

            var json = await ActionManager.ExecuteActionRaw("""
                (function() {
                    // too lazy to use getxxx
                    var infoContainer = document.querySelector(".image-info-container");

                    // get image url
                    var bgImage = infoContainer.querySelector(".track-coverart").style.backgroundImage;
                    var imgUrlStart = bgImage.indexOf('"') + 1;
                    var imgUrlEnd = bgImage.lastIndexOf('"');
                    var imgUrl = bgImage.substring(imgUrlStart, imgUrlEnd);
                    
                    // get song name and id
                    var titleAnchor = infoContainer.querySelector(".action-title");
                    var name = titleAnchor.innerText;
                    var id = parseInt(titleAnchor.href.substring(titleAnchor.href.lastIndexOf('/') + 1));

                    // get artist
                    var artistAnchor = infoContainer.querySelector(".action-artist");
                    var artist = artistAnchor.innerText;

                    // play details
                    var mainPlayer = document.querySelector(".main-player");
                    var playPauseCont = mainPlayer.querySelector(".play-pause-cont");
                    var playState = playPauseCont.children[0].classList[1]; // button name is the second class as of 12/7/2024

                    // durations
                    var durations = mainPlayer.querySelectorAll(".duration-text");
                    var durStart = "--", remainingTime = "--";
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
                """);

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