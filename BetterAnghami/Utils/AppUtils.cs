using Ookii.Dialogs.Wpf;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Threading;

namespace MRK
{
    public static class AppUtils
    {
        /// <summary>
        /// Prefix path for any resource
        /// </summary>
        public const string ResourcesPrefix = "MRK.Resources";

        /// <summary>
        /// The current app version, read from the assembly
        /// </summary>
        public static string AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

        /// <summary>
        /// Reads an embedded resosurce
        /// <para>Example: <em>MRK.Resources.CSS.BetterAnghami.css</em></para>
        /// </summary>
        public static async Task<string> ReadEmbeddedResource(string resourceName, bool appendPrefix = true)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                $"{(appendPrefix ? (ResourcesPrefix + '.') : string.Empty)}{resourceName}");

            using var reader = new StreamReader(stream!);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Generates an MD5 hash for the given input
        /// </summary>
        public static string GenerateMD5Hash(string raw)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Executes the given action using the provided control's <see cref="System.Windows.Threading.DispatcherObject" />
        /// </summary>
        public static void DispatchLater(DispatcherObject owner, Action action, int delay)
        {
            _ = Task.Delay(delay).ContinueWith(_ =>
            {
                owner.Dispatcher.Invoke(action);
            });
        }

        /// <summary>
        /// Displays a task dialog
        /// </summary>
        public static TaskDialogButton ShowDialog(
            string windowTitle = "",
            string mainInstruction = "",
            string content = "",
            string expandedInfo = "",
            ButtonType[]? buttons = null)
        {
            using var dialog = new TaskDialog
            {
                WindowTitle = windowTitle,
                MainInstruction = mainInstruction,
                Content = content,
                ExpandedInformation = expandedInfo
            };

            if (buttons != null && buttons.Length > 0)
            {
                foreach (var button in buttons)
                {
                    dialog.Buttons.Add(new TaskDialogButton(button));
                }
            }

            return dialog.ShowDialog();
        }
    }
}
