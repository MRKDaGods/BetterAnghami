using System.Collections;
using MRK.Models;

namespace MRK
{
    public class ThemePropertyList : IEnumerable<ThemeProperty>
    {
        private readonly List<ThemeProperty> _props = [];

        /// <summary>
        /// Copies all props from <c>other</c> into our own backing list
        /// </summary>
        public ThemePropertyList(List<ThemeProperty>? other)
        {
            if (other != null)
            {
                _props.AddRange(other);
            }
        }

        public ThemePropertyList() { }

        /// <summary>
        /// Build a list of <c>setProperty</c> calls for each <see cref="ThemeProperty" />
        /// </summary>
        public string BuildCssPropertyList(string stylePath = "document.documentElement.style")
        {
            return string.Join(
                ' ',
                _props.Select(x =>
                    $"{stylePath}.setProperty('{x.Name}','{JsUtils.EscapeSingleQuoted(x.Value)}');"
                )
            );
        }

        public IEnumerator<ThemeProperty> GetEnumerator()
        {
            return _props.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _props.GetEnumerator();
        }

        public static implicit operator ThemePropertyList(List<ThemeProperty>? list) => new(list);

        public static implicit operator List<ThemeProperty>?(ThemePropertyList? list) =>
            list?._props;
    }
}
