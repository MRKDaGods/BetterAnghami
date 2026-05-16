using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace MRK.Converters
{
    /// <summary>
    /// Converts a CSS variable name such as <c>--mrk-color-primary-a0</c>
    /// into a human-readable label like <c>Color Primary A0</c>.
    /// The leading <c>--mrk-</c> (or any <c>--prefix-</c>) is stripped,
    /// each word is title-cased, and hyphens become spaces.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public partial class FriendlyNameConverter : IValueConverter
    {
        // matches a leading "--word-" vendor prefix, e.g. "--mrk-" or "--ba-"
        [GeneratedRegex(@"^--[a-z]+-")]
        private static partial Regex VendorPrefixRegex();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string raw || string.IsNullOrWhiteSpace(raw))
                return value;

            // strip the leading -- (with or without a vendor prefix)
            var stripped = raw.StartsWith("--") ? VendorPrefixRegex().Replace(raw, string.Empty) : raw;

            // hyphens → spaces, then Title Case each word
            var words = stripped.Split('-', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', words.Select(w =>
                w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
