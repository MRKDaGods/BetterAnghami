using System.Text;
using System.Windows;
using MRK.Models;
using Ookii.Dialogs.Wpf;

namespace MRK
{
    /// <summary>
    /// Interaction logic for CssEditorWindow.xaml
    /// </summary>
    public partial class CssEditorWindow : Window
    {
        public ThemeMetadata Theme { get; init; }

        /// <summary>
        /// Opened theme's properties
        /// </summary>
        private List<ThemeProperty>? OriginalProperties { get; set; }

        /// <summary>
        /// Updated theme properties if changed
        /// </summary>
        public List<ThemeProperty>? NewProperties { get; private set; }

        /// <summary>
        /// ThemeManager instance
        /// </summary>
        private static ThemeManager ThemeManager => ThemeManager.Instance;

        public CssEditorWindow(ThemeMetadata theme, List<ThemeProperty>? props)
        {
            Theme = theme;
            OriginalProperties = props;

            InitializeComponent();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // load theme props from file if they werent supplied earlier
            if (OriginalProperties == null)
            {
                OriginalProperties = await ThemeManager.LoadTheme(Theme);
            }

            SetContent(OriginalProperties);
        }

        /// <summary>
        /// Fills the content textbox with the provided props
        /// </summary>
        private void SetContent(List<ThemeProperty>? props)
        {
            if (props == null)
            {
                contentTextbox.Text = "Cannot read theme properties.";
                UpdateStatusBar();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(":root {");

            foreach (var prop in props)
            {
                sb.AppendLine($"  {prop.Name}: {prop.Value};");
            }

            sb.AppendLine("}");

            contentTextbox.Text = sb.ToString();
            UpdateStatusBar();
        }

        /// <summary>
        /// Handles content changes and updates the status bar
        /// </summary>
        private void OnContentTextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e
        )
        {
            UpdateStatusBar();
        }

        /// <summary>
        /// Refreshes the status bar with line/char counts
        /// </summary>
        private void UpdateStatusBar()
        {
            var text = contentTextbox.Text ?? string.Empty;
            var lineCount =
                text.Length == 0 ? 0 : text.ReplaceLineEndings("\n").Count(c => c == '\n') + 1;
            var charCount = text.Length;

            statusBarLeft.Text = Theme.IsBuiltIn
                ? "Read-only: this theme cannot be edited"
                : "CSS: one variable per line inside :root { }";

            statusBarRight.Text = $"Lines: {lineCount}   Chars: {charCount}";
        }

        private void OnApplyClick(object sender, RoutedEventArgs e)
        {
            // set new props
            NewProperties = CssToThemePropertyConverter.Convert(
                contentTextbox.Text,
                out var unparsed
            );

            using var dialog = new TaskDialog
            {
                WindowTitle = "Better Anghami - CSS Editor",
                MainInstruction = $"Parsed {NewProperties.Count} variables",
                ExpandedInformation =
                    $"Unparsed lines{Environment.NewLine}{string.Join(Environment.NewLine, unparsed)}",
                ExpandFooterArea = true,
            };

            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));

            // show
            dialog.ShowDialog();
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            // remove new props
            NewProperties = null;

            // set original ones back
            SetContent(OriginalProperties);
        }
    }
}
