using MRK.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRK
{
    public enum WebViewEvent
    {
        None,
        DOMLoaded,
        SourceChanged,

        MAX
    }

    /// <summary>
    /// Stores the webview event action queue and action removal buffer
    /// </summary>
    internal class ActionStore
    {
        /// <summary>
        /// Pending actions
        /// </summary>
        public List<AsyncConsumableAction> Actions { get; init; }

        /// <summary>
        /// Actions to be removed post execution
        /// </summary>
        public HashSet<AsyncConsumableAction> RemovalBuffer { get; init; }

        public ActionStore()
        {
            Actions = [];
            RemovalBuffer = [];
        }
    }

    public class ActionManager
    {
        /// <summary>
        /// WebView events and their corresponding action stores
        /// </summary>
        private readonly Dictionary<WebViewEvent, ActionStore> _actions;

        private static ActionManager? _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ActionManager Instance
        {
            get => _instance ??= new ActionManager();
        }

        public ActionManager()
        {
            // initialize actions
            _actions = [];
            for (var wvEvent = WebViewEvent.None + 1; wvEvent < WebViewEvent.MAX; wvEvent++)
            {
                _actions[wvEvent] = new ActionStore();
            }
        }

        /// <summary>
        /// Checks if the supplied WebView event is valid
        /// </summary>
        private static bool IsWebViewEventValid(WebViewEvent webViewEvent)
        {
            return webViewEvent != WebViewEvent.None && webViewEvent != WebViewEvent.MAX;
        }

        /// <summary>
        /// Registers action to the corresponding WebView event store
        /// </summary>
        public void RegisterAction(WebViewEvent webViewEvent, AsyncConsumableAction action)
        {
            if (!IsWebViewEventValid(webViewEvent))
            {
                return;
            }

            _actions[webViewEvent].Actions.Add(action);
        }

        /// <summary>
        /// Executes all pending actions for an event
        /// </summary>
        /// <param name="webViewEvent">The WebView event for the actions</param>
        /// <param name="filter">An optional actions filter</param>
        /// <param name="removeActions">Remove actions in the removal buffer?</param>
        public async Task ExecuteActions(WebViewEvent webViewEvent, Func<AsyncConsumableAction, bool>? filter = null, bool removeActions = true)
        {
            if (!IsWebViewEventValid(webViewEvent))
            {
                return;
            }

            // corresponding action store
            var store = _actions[webViewEvent];

            var actions = store.Actions;
            if (filter != null)
            {
                actions = actions
                    .Where(filter)
                    .ToList();
            }

            // sort ascendingly by execution delay
            actions.Sort((x, y) => x.ExecutionDelay.CompareTo(y.ExecutionDelay));

            var executeStartTime = DateTime.Now;
            foreach (var action in actions)
            {
                // wait for delay (delay from time of event start)
                int delay = action.ExecutionDelay - (int)(DateTime.Now - executeStartTime).TotalMilliseconds;
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }

                // execute action
                await action.Execute();

                // should the action be removed?
                if (action.ShouldConsume())
                {
                    store.RemovalBuffer.Add(action);
                }
            }

            if (removeActions)
            {
                // remove actions in removal buffer
                foreach (var action in store.RemovalBuffer)
                {
                    store.Actions.Remove(action);
                }

                store.RemovalBuffer.Clear();
            }
        }

        /// <summary>
        /// Directly executes the provided script, and returns the result
        /// </summary>
        public async Task<string> ExecuteActionRaw(string script)
        {
            return await AnghamiWindow.Instance.WebView.ExecuteScriptAsync(script);
        }
    }
}
