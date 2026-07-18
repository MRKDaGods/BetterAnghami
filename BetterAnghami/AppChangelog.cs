namespace MRK
{
    public record VersionChangelog(string Version, string[] Changes);

    internal static class AppChangelog
    {
        // ordered newest to oldest
        private static readonly VersionChangelog[] _all =
        [
            new(
                "v0.3.2",
                [
                    "Player bar now restores on startup instead of only after re-logging in",
                    "Signed-out users are taken straight to the login page",
                    "Theme, Themes button and custom styles now reliably survive login and navigation",
                    "Currently-playing song shown in a distinct, readable colour across all themes",
                    "Fixed the Themes button appearing unstyled on newer Anghami builds",
                ]
            ),
            new(
                "v0.3.1",
                [
                    "App opens directly to login page; dark styling applied to login",
                    "Welcome popup shows on each new version with scrollable changelogs",
                    "Fixed CSS and UI elements being re-injected on every navigation",
                    "Fixed crash when actions were registered during execution",
                    "Loading screen background explicitly set before CSS loads",
                ]
            ),
            new(
                "v0.3.0",
                [
                    "Five dark themes: Midnight Abyss, Shadcn Dark, Nord, Dracula, Catppuccin Mocha",
                    "Redesigned loading screen with animated dots",
                    "Discord Rich Presence",
                    "Theme editor",
                ]
            ),
        ];

        public static VersionChangelog[] GetRecent(int count = 3) => _all.Take(count).ToArray();
    }
}
