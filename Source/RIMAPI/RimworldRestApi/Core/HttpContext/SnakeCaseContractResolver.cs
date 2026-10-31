using System.Text;
using Newtonsoft.Json.Serialization;

namespace RIMAPI.Core
{
    /// <summary>
    /// Turns a C# property name into the snake_case key every API answer and
    /// every request body uses. Lives in its own file so the unit tests can
    /// compile it against no game type at all (ticket #3316): the resolver is
    /// what shapes every field name the callers read, and a drift there is
    /// invisible until a caller reads a field that is no longer there.
    /// </summary>
    public class SnakeCaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return ConvertToSnakeCase(propertyName);
        }

        private string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder();
            result.Append(char.ToLower(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(input[i]));
                }
                else
                {
                    result.Append(input[i]);
                }
            }

            return result.ToString();
        }
    }
}
