using MRK.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MRK
{
    public class ThemeManager
    {
        private static ThemeManager? _instance;

        /// <summary>
        /// Currently installed themes' metadata
        /// <para>Call <c>LoadInstalledThemes()</c> first</para>
        /// </summary>
        public List<ThemeMetadata> InstalledThemes { get; private set; }

        /// <summary>
        /// FileManager instance
        /// </summary>
        private static FileManager FileManager => FileManager.Instance;

        public static ThemeManager Instance
        {
            get => _instance ??= new ThemeManager();
        }

        public ThemeManager()
        {
            InstalledThemes = [];
        }

        /// <summary>
        /// Loads an <b>installed</b> theme
        /// <para>Call <c>LoadInstalledThemes()</c> first</para>
        /// </summary>
        public async Task<List<ThemeProperty>?> LoadTheme(ThemeMetadata metadata)
        {
            // check if the given metadata is installed
            if (!InstalledThemes.Contains(metadata))
            {
                // not installed
                return null;
            }

            // go to backing store
            var backingStoreFileName = GetThemeBackingStoreName(metadata);
            if (!FileManager.Exists(backingStoreFileName))
            {
                return null;
            }

            // load file and parse json
            using var stream = FileManager.Open(backingStoreFileName, FileMode.Open);
            try
            {
                return await JsonSerializer.DeserializeAsync<List<ThemeProperty>>(stream) ?? [];
            }
            catch
            {
                // throw exception, how should we handle it later?
                throw new InvalidDataException("Invalid theme file");
            }
        }

        /// <summary>
        /// Loads all installed themes metadata into memory
        /// </summary>
        public async Task LoadInstalledThemes()
        {
            // clear old metadata
            InstalledThemes.Clear();

            // find themes file
            var installedThemesFileName = Configuration.Static.InstalledThemesFileName;
            if (!FileManager.Exists(installedThemesFileName))
            {
                return;
            }

            // load file and parse json
            using var stream = FileManager.Open(installedThemesFileName, FileMode.Open);

            try
            {
                // try parse insalled themes json
                InstalledThemes = await JsonSerializer.DeserializeAsync<List<ThemeMetadata>>(stream) ?? [];
            }
            catch
            {
                // back it up incase
                FileManager.Rename(installedThemesFileName, $"Invalid_{installedThemesFileName}.bak");

                // throw exception, how should we handle it later?
                throw new InvalidDataException("Invalid installed themes file");
            }
        }

        /// <summary>
        /// Returns the computed theme file name on disk
        /// </summary>
        private string GetThemeBackingStoreName(ThemeMetadata metadata)
        {
            return Utils.GenerateMD5Hash($"{metadata.Id}@{metadata.Version}");
        }
    }
}
