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

namespace MRK
{
    /// <summary>
    /// Main Window
    /// </summary>
    public partial class AnghamiWindow : Window
    {
        public CoreWebView2 WebView => webViewControl.CoreWebView2;
        private static ActionManager ActionManager => ActionManager.Instance;

#pragma warning disable CS8618 // Instance is never null
        public static AnghamiWindow Instance { get; private set; }
#pragma warning restore CS8618

        public AnghamiWindow()
        {
            // assign instance
            Instance = this;

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
        private async Task WaitForAnghamiLoad()
        {
            while (await WebView.ExecuteScriptAsync(Scripts.CheckForAnghamiBase()) == "-1")
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
        }
    }
}