using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MRK
{
    public static class Extensions
    {
        /// <summary>
        /// Converts a string to color
        /// </summary>
        public static Color ToColor(this string s, bool isRGBA = false)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(s);
                if (isRGBA && (s.Length == 5 || s.Length == 9)) // either #RGBA(5) or #RRGGBBAA(9)
                {
                    // r = a
                    // g = r
                    // b = g
                    // a = b
                    var tmp = color.A;
                    color.A = color.B;
                    color.B = color.G;
                    color.G = color.R;
                    color.R = tmp;
                }
                
                return color;
            }
            catch
            {
                return Colors.Transparent;
            }
        }

        /// <summary>
        /// Sets a RichTextBox text
        /// </summary>
        public static void SetText(this RichTextBox richTextBox, string text)
        {
            richTextBox.Document.Blocks.Clear();
            richTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
        }

        /// <summary>
        /// Current text in RichTextBox
        /// </summary>
        public static string GetText(this RichTextBox richTextBox)
        {
            return new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
        }

        /// <summary>
        /// Forcefully sets an ItemsControl's <c>ItemSource</c> property, and refreshes its children
        /// </summary>
        public static void ForceSetItemSource(this ItemsControl itemsControl, IEnumerable source)
        {
            itemsControl.ItemsSource = null;
            itemsControl.ItemsSource = source;
        }

        /// <summary>
        /// Returns the first child of type <typeparamref name="T"/>
        /// <para>https://stackoverflow.com/a/10279201/24518001</para>
        /// </summary>
        public static T? GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            
            return null;
        }
    }
}
