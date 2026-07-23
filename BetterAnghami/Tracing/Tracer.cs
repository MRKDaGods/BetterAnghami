using System.IO;

namespace MRK
{
    public enum TraceSeverity
    {
        Debug,
        Info,
        Warn,
        Error,
    }

    /// <summary>
    /// App-wide trace log. Writes timestamped lines to the log file under the app data folder
    /// (see <see cref="FileManager.BaseFolderPath"/>). Every write is guarded and eats its own
    /// failures, so tracing can never take the app down.
    /// </summary>
    public static class Tracer
    {
        /// <summary>
        /// Category tags for trace lines. Constants so call sites don't drift on spelling.
        /// </summary>
        public static class Category
        {
            public const string App = "App";
            public const string WebView = "WebView";
            public const string Action = "Action";
            public const string Rpc = "RPC";
            public const string Theme = "Theme";
            public const string Ui = "UI";
            public const string Config = "Config";
        }

        /// <summary>
        /// Lines below this level are dropped. Everything is captured by default.
        /// </summary>
        public static TraceSeverity MinimumLevel { get; set; } = TraceSeverity.Debug;

        /// <summary>
        /// Full path of the active log file, or null if tracing failed to start.
        /// </summary>
        public static string? LogFilePath { get; private set; }

        private static readonly object _sync = new();
        private static StreamWriter? _writer;
        private static bool _initialized;

        /// <summary>
        /// Opens the log file, rolling the previous session's log aside, and writes the session
        /// header. Only the first call does the work.
        /// </summary>
        public static void Initialize()
        {
            lock (_sync)
            {
                if (_initialized)
                {
                    return;
                }

                _initialized = true;

                try
                {
                    var folder = FileManager.Instance.BaseFolderPath;
                    var logPath = Path.Combine(folder, Configuration.Static.LogFileName);
                    var previousPath = Path.Combine(
                        folder,
                        Configuration.Static.PreviousLogFileName
                    );

                    // keep one prior session around, this session starts on a clean file.
                    // a failed roll (file locked, etc.) must not stop us from opening the new log
                    try
                    {
                        if (File.Exists(logPath))
                        {
                            File.Copy(logPath, previousPath, true);
                        }
                    }
                    catch
                    {
                        // couldn't roll the previous log, carry on with a fresh one
                    }

                    // share ReadWrite so the user (or a second instance) can open it while we run
                    _writer = new StreamWriter(
                        new FileStream(
                            logPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.ReadWrite
                        )
                    )
                    {
                        AutoFlush = true,
                    };

                    LogFilePath = logPath;
                }
                catch
                {
                    // couldn't open a log, run without tracing
                    _writer = null;
                    LogFilePath = null;
                }
            }

            WriteHeader();
        }

        public static void Debug(string category, string message)
        {
            Write(TraceSeverity.Debug, category, message, null);
        }

        public static void Info(string category, string message)
        {
            Write(TraceSeverity.Info, category, message, null);
        }

        public static void Warn(string category, string message, Exception? exception = null)
        {
            Write(TraceSeverity.Warn, category, message, exception);
        }

        public static void Error(string category, string message, Exception? exception = null)
        {
            Write(TraceSeverity.Error, category, message, exception);
        }

        private static void WriteHeader()
        {
            var bits = Environment.Is64BitProcess ? "x64" : "x86";

            Info(Category.App, "----- session start -----");
            Info(
                Category.App,
                $"BetterAnghami {AppUtils.AppVersion} ({bits}), OS {Environment.OSVersion.VersionString}, CLR {Environment.Version}"
            );
        }

        private static void Write(
            TraceSeverity level,
            string category,
            string message,
            Exception? exception
        )
        {
            if (level < MinimumLevel)
            {
                return;
            }

            lock (_sync)
            {
                if (_writer == null)
                {
                    return;
                }

                try
                {
                    var line =
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{LevelTag(level)}] [{category}] {message}";

                    if (exception != null)
                    {
                        line += Environment.NewLine + exception;
                    }

                    _writer.WriteLine(line);
                }
                catch
                {
                    // drop the line rather than surface a logging failure
                }
            }
        }

        private static string LevelTag(TraceSeverity level)
        {
            return level switch
            {
                TraceSeverity.Debug => "DBG",
                TraceSeverity.Info => "INF",
                TraceSeverity.Warn => "WRN",
                TraceSeverity.Error => "ERR",
                _ => "???",
            };
        }
    }
}
