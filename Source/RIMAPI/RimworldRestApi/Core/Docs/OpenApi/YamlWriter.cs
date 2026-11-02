using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RIMAPI.Core
{
    /// <summary>
    /// Writes a JSON tree as block-style YAML.
    /// </summary>
    /// <remarks>
    /// The usual C# route for an OpenAPI YAML is Microsoft.OpenApi's
    /// OpenApiYamlWriter, but that library would have to ship into the mod's
    /// Assemblies folder next to the game's own Newtonsoft (ticket #4234). The
    /// document is a tree of maps, sequences and scalars, so a writer of that
    /// subset is smaller than the dependency and stays testable.
    ///
    /// Strings are quoted whenever a plain scalar could be read as something
    /// else (a number, a boolean, null, a comment or a nested structure).
    /// </remarks>
    public static class YamlWriter
    {
        public static string Write(JToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            var sb = new StringBuilder();
            WriteNode(sb, token, 0);
            return sb.ToString();
        }

        private static void WriteNode(StringBuilder sb, JToken token, int indent)
        {
            if (token is JObject obj)
                WriteObject(sb, obj, indent);
            else if (token is JArray array)
                WriteArray(sb, array, indent);
            else
                sb.Append(Scalar(token)).Append('\n');
        }

        private static void WriteObject(StringBuilder sb, JObject obj, int indent)
        {
            if (obj.Count == 0)
            {
                sb.Append(Indent(indent)).Append("{}\n");
                return;
            }

            foreach (JProperty property in obj.Properties())
            {
                sb.Append(Indent(indent)).Append(Key(property.Name)).Append(':');
                WriteChild(sb, property.Value, indent);
            }
        }

        private static void WriteArray(StringBuilder sb, JArray array, int indent)
        {
            if (array.Count == 0)
            {
                sb.Append(Indent(indent)).Append("[]\n");
                return;
            }

            foreach (JToken item in array)
            {
                sb.Append(Indent(indent)).Append('-');
                if (item is JObject itemObject && itemObject.Count > 0)
                {
                    // The first key shares the dash's line, the rest align
                    // under it: "- name: x" then "  type: string".
                    bool first = true;
                    foreach (JProperty property in itemObject.Properties())
                    {
                        if (first)
                        {
                            sb.Append(' ').Append(Key(property.Name)).Append(':');
                            WriteChild(sb, property.Value, indent + 2);
                            first = false;
                        }
                        else
                        {
                            sb.Append(Indent(indent + 2)).Append(Key(property.Name)).Append(':');
                            WriteChild(sb, property.Value, indent + 2);
                        }
                    }
                }
                else if (item is JArray itemArray && itemArray.Count > 0)
                {
                    sb.Append('\n');
                    WriteArray(sb, itemArray, indent + 2);
                }
                else if (item is JObject emptyObject)
                {
                    sb.Append(" {}\n");
                }
                else if (item is JArray emptyArray)
                {
                    sb.Append(" []\n");
                }
                else
                {
                    sb.Append(' ').Append(Scalar(item)).Append('\n');
                }
            }
        }

        /// <summary>Writes the value of a key: inline for a scalar or empty container, else a nested block.</summary>
        private static void WriteChild(StringBuilder sb, JToken value, int indent)
        {
            if (value is JObject childObject)
            {
                if (childObject.Count == 0)
                {
                    sb.Append(" {}\n");
                    return;
                }

                sb.Append('\n');
                WriteObject(sb, childObject, indent + 2);
                return;
            }

            if (value is JArray childArray)
            {
                if (childArray.Count == 0)
                {
                    sb.Append(" []\n");
                    return;
                }

                sb.Append('\n');
                WriteArray(sb, childArray, indent + 2);
                return;
            }

            sb.Append(' ').Append(Scalar(value)).Append('\n');
        }

        private static string Scalar(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return "null";

            if (token.Type == JTokenType.String)
                return ScalarString((string)token);

            if (token.Type == JTokenType.Boolean)
                return (bool)token ? "true" : "false";

            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture);

            return ScalarString(token.ToString());
        }

        private static string ScalarString(string value)
        {
            if (value == null)
                return "null";

            if (!NeedsQuotes(value))
                return value;

            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string Key(string name)
        {
            if (name == null)
                throw new ArgumentException("A YAML key cannot be null.", nameof(name));
            return NeedsQuotes(name) ? ScalarString(name) : name;
        }

        /// <summary>Whether the text must be quoted to stay the string it is.</summary>
        private static bool NeedsQuotes(string value)
        {
            if (value.Length == 0)
                return true;

            if (value != value.Trim())
                return true;

            string lower = value.ToLowerInvariant();
            if (lower == "null" || lower == "~" || lower == "true" || lower == "false"
                || lower == "yes" || lower == "no" || lower == "on" || lower == "off")
                return true;

            if (LooksNumeric(value))
                return true;

            const string structural = ":#{}[],&*!|>'\"%@`\n\r\t\\";
            foreach (char c in value)
            {
                if (structural.IndexOf(c) >= 0)
                    return true;
            }

            return "-?".IndexOf(value[0]) >= 0;
        }

        private static bool LooksNumeric(string value)
        {
            if (value.Length == 0)
                return false;

            int i = value[0] == '-' || value[0] == '+' ? 1 : 0;
            if (i == value.Length)
                return false;

            bool digit = false;
            bool dot = false;
            for (; i < value.Length; i++)
            {
                char c = value[i];
                if (c >= '0' && c <= '9')
                    digit = true;
                else if (c == '.' && !dot)
                    dot = true;
                else
                    return false;
            }
            return digit;
        }

        private static string Indent(int width) => new string(' ', width);
    }
}
