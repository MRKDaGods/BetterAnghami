namespace MRK
{
    /// <summary>
    /// Escaping helpers for baking C# strings into generated JavaScript.
    /// </summary>
    public static class JsUtils
    {
        /// <summary>
        /// Escapes text for a JS template literal (<c>`...`</c>): backslash, backtick and <c>${</c>.
        /// Newlines are left as-is since template literals are multi-line.
        /// </summary>
        public static string EscapeTemplateLiteral(string value)
        {
            return value.Replace("\\", "\\\\").Replace("`", "\\`").Replace("${", "\\${");
        }

        /// <summary>
        /// Escapes text for a single-quoted JS string (<c>'...'</c>): backslash, the quote and newlines.
        /// </summary>
        public static string EscapeSingleQuoted(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
