namespace MRK
{
    public record VersionChangelog(string Version, string[] Changes);

    internal static class AppChangelog
    {
        // ordered newest to oldest
        private static readonly VersionChangelog[] _all =
        [
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
