using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MRK
{
    public readonly struct ConfigurationRecord(string value)
    {
        public string String { get; init; } = value;
        public readonly int Int => AsInt();
        public readonly float Float => AsFloat();

        private readonly int AsInt()
        {
            _ = int.TryParse(String, out int i);
            return i;
        }

        private readonly float AsFloat()
        {
            _ = float.TryParse(String, out float f);
            return f;
        }

        public static implicit operator ConfigurationRecord(string value)
        {
            return new ConfigurationRecord(value);
        }

        public static implicit operator string(ConfigurationRecord v)
        {
            return v.String;
        }
    }

    public class Configuration
    {
        public static class Static
        {
            /// <summary>
            /// Base folder name used for storing app data
            /// </summary>
            public const string BaseFolderName = "BetterAnghami";

            /// <summary>
            /// Installed themes relative file name
            /// </summary>
            public const string InstalledThemesFileName = "InstalledThemes.json";

            /// <summary>
            /// Where the theme backing stores will be stored at
            /// </summary>
            public const string ThemeBackingStoreDirectory = "ThemeStore";

            /// <summary>
            /// Configuration relative file name
            /// </summary>
            public const string ConfigFileName = "Config.json";
        }

        public static class Keys
        {
            public const string SelectedThemeId = "selectedThemeId";
        }

        private Dictionary<string, string> _config;

        private static Configuration? _instance;

        private static FileManager FileManager => FileManager.Instance;

        public static Configuration Instance
        {
            get => _instance ??= new Configuration();
        }

        public Configuration()
        {
            _config = [];

            // try load config
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (!FileManager.Exists(Static.ConfigFileName))
            {
                return;
            }

            try
            {
                using var stream = FileManager.Open(Static.ConfigFileName, FileMode.Open);
                _config = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
            }
            catch
            {
                // load failed
                // do nothing
            }
        }

        private void SaveConfig()
        {
            lock (_config)
            {
                try
                {
                    using var stream = FileManager.Open(Static.ConfigFileName, FileMode.Create);
                    JsonSerializer.Serialize(stream, _config);
                }
                catch
                {
                    // save failed
                    // do nothing
                }
            }
        }

        /// <summary>
        /// Gets the configuration record associated with <paramref name="key"/>
        /// <para>An empty record is returned if key is not found</para>
        /// </summary>
        public ConfigurationRecord this[string key]
        {
            get
            {
                _config.TryGetValue(key, out var val);
                return new ConfigurationRecord(val ?? string.Empty);
            }

            set
            {
                _config[key] = value;

                // save config on another thread
                Task.Run(SaveConfig);
            }
        }
    }
}
