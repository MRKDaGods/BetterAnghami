using MRK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace MRK
{
    public partial class ColorUtility
    {
        public static List<ThemeColor> MatchColors(string text)
        {
            List<ThemeColor> result = [];

            // regex
            // https://stackoverflow.com/a/63856391/24518001
            Regex[] expressions = [HexColorRegex(), HSLColorRegex(), RGBColorRegex()];

            for (int i = 0; i < expressions.Length; i++)
            {
                foreach (var match in expressions[i].EnumerateMatches(text))
                {
                    result.Add(
                        new ThemeColor(match.Index, match.Length, text.Substring(match.Index, match.Length), (ThemeColorType)(i + 1))
                    );
                }
            }

            return result;
        }

        /// <summary>
        /// Converts RGB[A] string to color
        /// </summary>
        public static Color ConvertRGBToColor(string rgb)
        {
            rgb = rgb.Trim();

            int start = rgb.IndexOf('(');
            int end = rgb.IndexOf(')');

            if (start < 0 || end < 0 || end < start)
            {
                return Colors.Transparent;
            }

            var components = rgb.Substring(start + 1, end - start - 1)
                .Split(',');

            var r = byte.Parse(components[0]);
            var g = byte.Parse(components[1]);
            var b = byte.Parse(components[2]);

            switch (components.Length)
            {
                case 3:
                    return Color.FromRgb(r, g, b);

                case 4:
                    float a = float.Parse(components[3]);
                    return Color.FromArgb((byte)(a * 255), r, g, b);

                default:
                    return Colors.Transparent;
            }
        }

        [GeneratedRegex("#[a-f\\d]{3}(?:[a-f\\d]?|(?:[a-f\\d]{3}(?:[a-f\\d]{2})?)?)\\b", RegexOptions.IgnoreCase)]
        private static partial Regex HexColorRegex();

        [GeneratedRegex("hsla?\\((?:(-?\\d+(?:deg|g?rad|turn)?),\\s*((?:\\d{1,2}|100)%),\\s*((?:\\d{1,2}|100)%)(?:,\\s*((?:\\d{1,2}|100)%|0(?:\\.\\d+)?|1))?|(-?\\d+(?:deg|g?rad|turn)?)\\s+((?:\\d{1,2}|100)%)\\s+((?:\\d{1,2}|100)%)(?:\\s+((?:\\d{1,2}|100)%|0(?:\\.\\d+)?|1))?)\\)", RegexOptions.IgnoreCase)]
        private static partial Regex HSLColorRegex();

        [GeneratedRegex("rgba?\\((?:(25[0-5]|2[0-4]\\d|1?\\d{1,2}|(?:\\d{1,2}|100)%),\\s*(25[0-5]|2[0-4]\\d|1?\\d{1,2}|(?:\\d{1,2}|100)%),\\s*(25[0-5]|2[0-4]\\d|1?\\d{1,2}|(?:\\d{1,2}|100)%)(?:,\\s*((?:\\d{1,2}|100)%|0(?:\\.\\d+)?|1))?|(25[0-5]|2[0-4]\\d|1?\\d{1,2}|(?:\\d{1,2}|100)%)\\s+(25[0-5]|2[0-4]\\d|1?\\d{1,2}|(?:\\d{1,2}|100)%)\\s+(25[0-5]|2[0-4]\\d|1?\\d{1,2}|(?:\\d{1,2}|100)%)(?:\\s+((?:\\d{1,2}|100)%|0(?:\\.\\d+)?|1))?)\\)", RegexOptions.IgnoreCase)]
        private static partial Regex RGBColorRegex();
    }
}
