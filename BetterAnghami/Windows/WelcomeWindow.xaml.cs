using System.Windows;

namespace MRK
{
    public partial class WelcomeWindow : Window
    {
        public string Subtitle { get; }
        public string VersionText { get; }
        public VersionChangelog[] Changelogs { get; }
        public string ButtonText { get; }

        public WelcomeWindow(bool isLoggedIn, bool isFirstLaunch)
        {
            Subtitle = isFirstLaunch ? "Welcome" : "What's New";
            VersionText = $"v{AppUtils.AppVersion}";
            Changelogs = AppChangelog.GetRecent();
            ButtonText = isLoggedIn ? "Got it" : "Get Started";

            DataContext = this;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void OnActionClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
