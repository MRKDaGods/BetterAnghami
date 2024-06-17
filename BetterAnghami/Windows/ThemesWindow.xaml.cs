using MRK.Models;
using System.Windows;
using System.Collections.Generic;

namespace MRK
{
    /// <summary>
    /// Interaction logic for ThemesWindow.xaml
    /// </summary>
    public partial class ThemesWindow : Window
    {
        /// <summary>
        /// ThemeManager instance
        /// </summary>
        private static ThemeManager ThemeManager => ThemeManager.Instance;

        //private static readonly List<ThemeMetadata> _mockThemes = [
        //    new ThemeMetadata("Dark", "Anghami", "Default dark theme"),
        //    new ThemeMetadata("Light", "Anghami", "Default light theme"),
        //    new ThemeMetadata("Lights Out", "Mohamed Ammar", "It's pretty dark in here")
        //];

        public ThemesWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Window loaded event handler
        /// </summary>
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // load themes
            await ThemeManager.LoadInstalledThemes();

            // set items source
            themesControl.ItemsSource = ThemeManager.InstalledThemes;

            // show no themes available if empty
            noThemesLabel.Visibility = ThemeManager.InstalledThemes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // hide loading label
            loadingLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Create themes click handler
        /// </summary>
        private void OnCreateThemeClick(object sender, RoutedEventArgs e)
        {
            // show create theme window
            new CreateThemeWindow().ShowDialog();
        }
    }
}
