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
            var user = await AnghamiWindow.Instance.GetLocalUser();

            // set creator as local user, and disable the corresponding textboxes

            // user id
            creatorIdTextBox.Text = user.Id.ToString();
            creatorIdTextBox.IsEnabled = false;

            // user name
            creatorNameTextBox.Text = user.Name;
            creatorNameTextBox.IsEnabled = false;
        }

        /// <summary>
        /// Create button click event handler
        /// </summary>
        private void OnCreateClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
