using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MRK
{
    public static class Utils
    {
        /// <summary>
        /// Prefix path for any resource
        /// </summary>
        public const string ResourcesPrefix = "MRK.Resources";

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
            var sb = new StringBuilder();

            foreach (var b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Executes the given action using the provided control's <see cref="System.Windows.Threading.DispatcherObject" />
        /// </summary>
        public static void DispatchLater(DispatcherObject owner, Action action, int delay)
        {
            Task.Delay(delay).ContinueWith(_ =>
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
