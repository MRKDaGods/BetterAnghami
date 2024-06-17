using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MRK
{
    public static class Utils
    {
        /// <summary>
        /// Reads an embedded resosurce
        /// <para>Example: <em>MRK.CSS.BetterAnghami.css</em></para>
        /// </summary>
        public static async Task<string> ReadEmbeddedResource(string resourceName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream!);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Generates an MD5 hash for the given input
        /// </summary>
        public static string GenerateMD5Hash(string raw)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            var sb = new StringBuilder();

            foreach (var b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
