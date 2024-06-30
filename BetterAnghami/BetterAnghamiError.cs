namespace MRK
{
    public enum BetterAnghamiError
    {
        None = 0,

        // Theme errors 0x1000
        ThemeError = 0x1000,
        ThemeAlreadyInstalled,
        InstalledThemesNotWrittenToDisk,
        ThemeBackingStoreNotWrittenToDisk,
        ThemeCannotBeRemoved,
        ThemeNotInstalled,
    }
}
