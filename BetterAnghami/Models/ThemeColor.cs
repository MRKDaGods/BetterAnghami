using System.Windows.Media;

namespace MRK.Models
{
    public enum ThemeColorType
    {
        None,
        Hex,
        HSL,
        RGB
    }

    public class ThemeColor
    {
        private Color? _color;

        public int Start { get; set; }
        public int Length { get; private set; }
        public string Value { get; private set; } = string.Empty;
        public ThemeColorType Type { get; private set; }
        public ThemeProperty? Owner { get; set; }
        public Color Color => _color ??= ResolveColor();

        public ThemeColor(int start, int length, string value, ThemeColorType type)
        {
            InternalConstruct(start, length, value, type);
        }

        public void InternalConstruct(int start, int length, string value, ThemeColorType type)
        {
            Start = start;
            Length = length;
            Value = value;
            Type = type;
            
            _color = null;
        }

        private Color ResolveColor()
        {
            if (Type == ThemeColorType.None || string.IsNullOrWhiteSpace(Value)) 
            {
                return Colors.Transparent;
            }

            switch (Type)
            {
                case ThemeColorType.Hex: // css colors take #R[R]G[G]B[B]A[A]
                    return Value.ToColor(true);

                case ThemeColorType.RGB:
                    return ColorUtility.ConvertRGBToColor(Value);

                // Currently unsupported
                case ThemeColorType.HSL:
                    return Colors.Transparent;
            }

            return Colors.Transparent;
        }
    }
}
