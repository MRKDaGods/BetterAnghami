using MRK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace MRK
{
    public class ThemeManager
    {
        /// <summary>
        /// Currently installed themes' metadata
        /// <para>Call <see cref="LoadInstalledThemes" /> first</para>
        /// </summary>
        public List<ThemeMetadata> InstalledThemes { get; private set; }

        /// <summary>
        /// Currently selected theme
        /// </summary>
        public ThemeMetadata SelectedTheme
        {
            get => GetSelectedTheme();
            set => SetSelectedTheme(value);
        }

        /// <summary>
        /// Installed themes have changed event
        /// </summary>
        public event Action? ThemesChanged;

        private static ThemeManager? _instance;

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
        /// <para>Call <see cref="LoadInstalledThemes" /> first</para>
        /// </summary>
        public async Task<List<ThemeProperty>?> LoadTheme(ThemeMetadata metadata)
        {
            // check if the given metadata is installed
            if (!InstalledThemes.Contains(metadata))
            {
                // not installed
                return null;
            }

            // load from resources in case of builtin
            if (metadata.IsBuiltIn)
            {
                return await GetBuiltInThemeProperties(metadata.Name);
            }

            // go to backing store
            var backingStoreFileName = GetThemeBackingStoreName(metadata);
            if (!FileManager.Exists(backingStoreFileName))
            {
                return null;
            }

            try
            {
                // load file and parse json
                using var stream = FileManager.Open(backingStoreFileName, FileMode.Open);

                // deserialize
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
            // get built in themes
            InstalledThemes = await GetBuiltInThemes();

            // find themes file
            var installedThemesFileName = Configuration.Static.InstalledThemesFileName;
            if (!FileManager.Exists(installedThemesFileName))
            {
                // raise event handler
                RaiseThemesChangedEvent();
                return;
            }

            try
            {
                // load file and parse json
                using var stream = FileManager.Open(installedThemesFileName, FileMode.Open);

                // try parse insalled themes json
                var installed = await JsonSerializer.DeserializeAsync<List<ThemeMetadata>>(stream);
                if (installed != null)
                {
                    InstalledThemes.AddRange(installed);
                }
            }
            catch
            {
                // back it up incase
                FileManager.Rename(installedThemesFileName, $"Invalid_{installedThemesFileName}.bak");

                // throw exception, how should we handle it later?
                throw new InvalidDataException("Invalid installed themes file");
            }
            finally
            {
                // raise event handler
                RaiseThemesChangedEvent();
            }
        }

        /// <summary>
        /// Removes an installed themes
        /// </summary>
        public async Task<BetterAnghamiError> RemoveTheme(ThemeMetadata metadata)
        {
            // built-in themes cant be removed
            if (metadata.IsBuiltIn)
            {
                return BetterAnghamiError.ThemeCannotBeRemoved;
            }

            if (!InstalledThemes.Contains(metadata))
            {
                return BetterAnghamiError.ThemeNotInstalled;
            }

            // remove from installed themes
            InstalledThemes.Remove(metadata);

            // raise event
            RaiseThemesChangedEvent();

            // write to disk
            if (!await WriteInstalledThemesMetadataToDisk())
            {
                return BetterAnghamiError.InstalledThemesNotWrittenToDisk;
            }

            // remove backing store
            var storeName = GetThemeBackingStoreName(metadata);
            if (FileManager.Exists(storeName))
            {
                FileManager.Delete(storeName);
            }

            return BetterAnghamiError.None;
        }

        /// <summary>
        /// Returns the computed theme file name on disk
        /// </summary>
        private string GetThemeBackingStoreName(ThemeMetadata metadata)
        {
            const string storeDir = Configuration.Static.ThemeBackingStoreDirectory;
            if (!FileManager.DirectoryExists(storeDir))
            {
                FileManager.DirectoryCreate(storeDir);
            }

            return $"{storeDir}/{Utils.GenerateMD5Hash($"{metadata.Id}@{metadata.Version}")}";
        }

        /// <summary>
        /// Gets a builtin theme's properties
        /// </summary>
        /// <param name="themeName">File name of theme located in Resources/CSS/Themes/<b>themeName</b>.css</param>
        private async Task<List<ThemeProperty>> GetBuiltInThemeProperties(string themeName)
        {
            // remove all whitespaces
            themeName = themeName.Replace(" ", "");

            // read built in theme
            var css = await Utils.ReadEmbeddedResource($"CSS.Themes.{themeName}.css");

            // convert to props
            return CssToThemePropertyConverter
                .Convert(css, out _)
                .Select(x => new ThemeProperty(x.Item1, x.Item2))
                .ToList();
        }

        /// <summary>
        /// Creates a new theme from the given metadata based on the default dark theme
        /// </summary>
        public async Task<BetterAnghamiError> CreateTheme(ThemeMetadata metadata)
        {
            // read built in dark theme
            var themeProps = await GetBuiltInThemeProperties("DefaultDark");

            // install the theme
            var result = await InstallTheme(metadata, themeProps);

            // raise event handler
            RaiseThemesChangedEvent();

            return result;
        }

        /// <summary>
        /// Installs a theme and writes it to disk
        /// </summary>
        /// <param name="installBackingStoreOnly">Should we only install the backing store?</param>
        public async Task<BetterAnghamiError> InstallTheme(ThemeMetadata metadata, List<ThemeProperty> props, bool installBackingStoreOnly = false)
        {
            if (!installBackingStoreOnly)
            {
                // check if theme is already installed
                if (InstalledThemes.Contains(metadata))
                {
                    return BetterAnghamiError.ThemeAlreadyInstalled;
                }

                // add metadata to installed themes
                InstalledThemes.Add(metadata);

                // write installed themes to disk
                if (!await WriteInstalledThemesMetadataToDisk())
                {
                    return BetterAnghamiError.InstalledThemesNotWrittenToDisk;
                }
            }

            // convert to json and write to disk
            var backingStoreFileName = GetThemeBackingStoreName(metadata);
            try
            {
                using var stream = FileManager.Open(backingStoreFileName, FileMode.Create);
                await JsonSerializer.SerializeAsync(stream, props);
            }
            catch
            {
                return BetterAnghamiError.ThemeBackingStoreNotWrittenToDisk;
            }

            // create backing store from default dark theme
            return BetterAnghamiError.None;
        }

        /// <summary>
        /// Attempts to read the ThemeMetadata json from a theme css file
        /// </summary>
        public static ThemeMetadata? GetMetadataFromCss(string css)
        {
            var buf = string.Empty;

            ThemeMetadata? metadata = null;

            // consumer is a function that gets executed after each buffer insertion
            // to mark metadata start and end
            Action<ObjectReference<bool>> consumer = (_) =>
            {
                const string MetadaraStartTag = "<metadata>";
                const string MetadataEndTag = "</metadata>";

                // check for start
                if (buf.EndsWith(MetadaraStartTag)) // metadata start 
                {
                    buf = string.Empty;

                    // update consumer to find metadata close
                    consumer = (exit) =>
                    {
                        // check for end
                        if (buf.EndsWith(MetadataEndTag)) // metadata end tag
                        {
                            var json = buf.Substring(0, buf.Length - MetadataEndTag.Length);
                            try
                            {
                                metadata = JsonSerializer.Deserialize<ThemeMetadata>(json);
                            }
                            catch
                            {
                                // exit loop
                                exit.Value = true;
                            }
                        }
                    };
                }
            };

            // init reader
            using var reader = new StringReader(css);

            var exitRef = new ObjectReference<bool>(false);
            while (!exitRef.Value && reader.Peek() != -1)
            {
                buf += (char)reader.Read();

                // call consumer
                consumer(exitRef);
            }

            return metadata;
        }

        /// <summary>
        /// Gets the built in themes' metadata
        /// </summary>
        private async Task<List<ThemeMetadata>> GetBuiltInThemes()
        {
            // built in themes are located in MRK/Resources/CSS/Themes
            const string themesPrefix = $"{Utils.ResourcesPrefix}.CSS.Themes";

            List<ThemeMetadata> themes = [];

            var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var name in resourceNames)
            {
                if (name.StartsWith(themesPrefix))
                {
                    var css = await Utils.ReadEmbeddedResource(name, false);
                    var metadata = GetMetadataFromCss(css);

                    if (metadata != null)
                    {
                        themes.Add(metadata);
                    }
                }
            }

            return themes;
        }

        /// <summary>
        /// Writes <see cref="InstalledThemes"/> to disk
        /// </summary>
        private async Task<bool> WriteInstalledThemesMetadataToDisk()
        {
            try
            {
                // open stream
                using var installedThemesFile = FileManager.Open(Configuration.Static.InstalledThemesFileName, FileMode.Create);

                // dont write built in themes to disk
                var themesToWrite = InstalledThemes
                    .Where(x => !x.IsBuiltIn)
                    .ToList();

                // write to stream
                await JsonSerializer.SerializeAsync(installedThemesFile, themesToWrite);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Raises the themes change event handler
        /// </summary>
        private void RaiseThemesChangedEvent()
        {
            ThemesChanged?.Invoke();
        }

        /// <summary>
        /// Returns the currently selected theme
        /// <para>The first built-in theme is returned incase of invalid old selected theme</para>
        /// </summary>
        private ThemeMetadata GetSelectedTheme()
        {
            string selectedThemeId = Configuration.Instance[Configuration.Keys.SelectedThemeId];

            // check if theme is installed
            ThemeMetadata? targetTheme;
            if (string.IsNullOrEmpty(selectedThemeId) ||
                (targetTheme = InstalledThemes.Find(x => x.Id == selectedThemeId)) == null)
            {
                // select first built in theme
                targetTheme = InstalledThemes[0];

                // store it
                SetSelectedTheme(targetTheme);
            }

            return targetTheme;
        }

        /// <summary>
        /// Sets the selected theme, and saves it locally
        /// </summary>
        private void SetSelectedTheme(ThemeMetadata theme)
        {
            if (!InstalledThemes.Contains(theme))
            {
                throw new ArgumentException("Trying to set a non-installed theme");
            }

            // simple
            Configuration.Instance[Configuration.Keys.SelectedThemeId] = theme.Id;
        }
    }
}
