using MRK.Models;
using System;
using System.Windows;

namespace MRK
{
    /// <summary>
    /// Interaction logic for CreateThemeWindow.xaml
    /// </summary>
    public partial class CreateThemeWindow : Window
    {
        public CreateThemeWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Window loaded event handler
        /// </summary>
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // get current user
            // in theory, local user should never be null
            try
            {
                var user = await AnghamiWindow.Instance.GetLocalUser();

                // set creator as local user, and disable the corresponding textboxes

                // user id
                creatorIdTextBox.Text = user.Id.ToString();
                creatorIdTextBox.IsEnabled = false;

                // user name
                creatorNameTextBox.Text = user.Name;
                creatorNameTextBox.IsEnabled = false;
            }
            catch
            {
                // cannot get local user

                // disable all inputs
                creatorIdTextBox.IsEnabled =
                    creatorNameTextBox.IsEnabled =
                    themeNameTextBox.IsEnabled =
                    themeDescTextBox.IsEnabled =
                    themeVersionTextBox.IsEnabled = false;

                // disable create button
                createButton.IsEnabled = false;

                // set error text
                SetError("Cannot get local user, are you logged in?");
            }
        }

        /// <summary>
        /// Sets the error text, and controls its visiblity accordingly
        /// </summary>
        private void SetError(string error)
        {
            errorText.Text = error;
            errorText.Visibility = string.IsNullOrWhiteSpace(error) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Create button click event handler
        /// </summary>
        private async void OnCreateClick(object sender, RoutedEventArgs e)
        {
            // basic validation

            // simple check for id
            if (!int.TryParse(creatorIdTextBox.Text, out int creatorId))
            {
                SetError("Invalid creator ID");
                return;
            }

            // creator name
            if (string.IsNullOrWhiteSpace(creatorNameTextBox.Text))
            {
                SetError("Invalid creator name");
                return;
            }

            // theme name
            if (string.IsNullOrWhiteSpace(themeNameTextBox.Text))
            {
                SetError("Invalid theme name");
                return;
            }

            // theme desc
            if (themeDescTextBox.Text.Length > 256)
            {
                SetError("Theme description too long");
                return;
            }

            if (!Version.TryParse(themeVersionTextBox.Text, out Version? version))
            {
                SetError("Invalid version");
                return;
            }

            var metadata = new ThemeMetadata(
                    Guid.NewGuid().ToString("N"),
                    themeNameTextBox.Text.Trim(),
                    creatorId,
                    creatorNameTextBox.Text.Trim(),
                    themeDescTextBox.Text.Trim(),
                    version.ToString());

            var error = await ThemeManager.Instance.CreateTheme(metadata);
            if (error != BetterAnghamiError.None)
            {
                // display error
                SetError($"An error has occurred {error}");
                return;
            }

            // done
            Close();
        }
    }
}
