using MRK.Models;
using System.Text.RegularExpressions;

namespace MRK
{
    public partial class CssToThemePropertyConverter
    {
        /// <summary>
        /// Converts a set of CSS variables to theme properties
        /// </summary>
        public static List<ThemeProperty> Convert(string css, out List<string> unparsedLines)
        {
            unparsedLines = [];

            List<ThemeProperty> result = [];

            // remove comments
            // https://stackoverflow.com/a/10555656/24518001
            // regex: [/][*]([^*]|[*]*[^*/])*[*]+[/]
            // replace with new lines to avoid multiline comment bug
            // --secondary: #6c757d; /*
            // lol */ --success: #28a745;
            css = CssCommentRegex().Replace(css, "\n");
            
            // fix incomplete floats
            ReplaceIncompleteFloatingPoints(ref css);

            // Split into lines
            var lines = css.ReplaceLineEndings().Split(Environment.NewLine).AsSpan();

            foreach (ref string line in lines)
            {
                line = line.Trim();
                
                // skip empty lines without adding them to the unparsed list
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // only parse vars
                if (!line.StartsWith("--"))
                {
                    unparsedLines.Add(line);
                    continue;
                }

                int seperatorIdx = line.IndexOf(':');
                if (seperatorIdx == -1)
                {
                    unparsedLines.Add(line);
                    continue;
                }

                // find last ; for cases like background-image: url("data:image/png;base64, ...
                int terminatorIdx = line.LastIndexOf(';');
                if (terminatorIdx == -1 || terminatorIdx < seperatorIdx)
                {
                    unparsedLines.Add(line);
                    continue;
                }

                var name = line.Substring(0, seperatorIdx).Trim();
                var value = line.Substring(seperatorIdx + 1, terminatorIdx - seperatorIdx - 1).Trim();

                result.Add(new ThemeProperty(name, value));
            }

            return result;
        }

        /// <summary>
        /// Replaces incomplete floating point numbers in the form of "<b>.2</b>[3..]"
        /// </summary>
        public static void ReplaceIncompleteFloatingPoints(ref string str)
        {
            str = IncompleteFloatingPointRegex().Replace(str, m => '0' + m.Value );
        }

        [GeneratedRegex("[/][*]([^*]|[*]*[^*/])*[*]+[/]", RegexOptions.Multiline)]
        private static partial Regex CssCommentRegex();

        [GeneratedRegex("(?<!\\d)\\.\\d+", RegexOptions.Multiline)]
        private static partial Regex IncompleteFloatingPointRegex();
    }
}
