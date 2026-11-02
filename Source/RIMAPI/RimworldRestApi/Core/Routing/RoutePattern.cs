using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RIMAPI.Core
{
    /// <summary>
    /// Turns a route pattern into the regex the router matches requests with.
    /// </summary>
    /// <remarks>
    /// The old conversion escaped the pattern and replaced the escapes, but
    /// .NET's Regex.Escape leaves '}' and ':' alone, so a pattern with a
    /// parameter became an invalid group name and the route threw while it was
    /// being registered (ticket #4234: /api/v1/docs/extensions/{extensionId}
    /// answered 404 and took the rest of its controller's routes with it).
    /// Plain C#, so the conversion is tested without the game.
    /// </remarks>
    public static class RoutePattern
    {
        public const string ParameterPrefix = "param_";

        /// <summary>An anchored regex; every {name} or :name becomes a param_ group.</summary>
        public static string ToRegex(string pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            var sb = new StringBuilder(pattern.Length + 16);
            sb.Append('^');

            int i = 0;
            while (i < pattern.Length)
            {
                char c = pattern[i];

                if (c == '{')
                {
                    int close = pattern.IndexOf('}', i + 1);
                    if (close > i + 1)
                    {
                        AppendParameter(sb, pattern.Substring(i + 1, close - i - 1));
                        i = close + 1;
                        continue;
                    }
                }
                else if (c == ':')
                {
                    int start = i + 1;
                    int end = start;
                    while (end < pattern.Length && (char.IsLetterOrDigit(pattern[end]) || pattern[end] == '_'))
                        end++;
                    if (end > start)
                    {
                        AppendParameter(sb, pattern.Substring(start, end - start));
                        i = end;
                        continue;
                    }
                }

                sb.Append(Regex.Escape(c.ToString()));
                i++;
            }

            sb.Append('$');
            return sb.ToString();
        }

        private static void AppendParameter(StringBuilder sb, string name)
        {
            var clean = new StringBuilder(name.Length);
            foreach (char c in name)
                clean.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

            if (clean.Length == 0)
                throw new ArgumentException("A route parameter needs a name.", nameof(name));

            sb.Append("(?<").Append(ParameterPrefix).Append(clean).Append(">[^/]+)");
        }
    }
}
