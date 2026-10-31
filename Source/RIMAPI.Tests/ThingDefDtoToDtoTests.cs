using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// ThingDefDto.ToDto has to assign every member its class declares (ticket
    /// #3442). The five armor and insulation members were declared and never
    /// filled, so /game/defs/things answered 0 for every def and a reader could
    /// not tell "no armor" from "not filled in"; TechLevel, TradeTags and
    /// StuffCategories sat in the same state. A missing assignment shows up
    /// nowhere in the JSON, so the test reads the source: the member list of the
    /// class and the assignment lines inside ToDto.
    /// </summary>
    public class ThingDefDtoToDtoTests
    {
        // The member shapes ModelKeyTests reads, so a member the JSON scan sees
        // is a member this scan sees.
        private static readonly Regex PropertyDeclaration = new Regex(
            @"^\s*public\s+(?!(?:static|const|class|enum|struct|interface|delegate|event|override|virtual|abstract)\b)"
                + @"[^;{}=()]+?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex FieldDeclaration = new Regex(
            @"^\s*public\s+(?!(?:static|const|class|enum|struct|interface|delegate|event|override|virtual|abstract)\b)"
                + @"[^;{}=()]+?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:=[^;]*)?;\s*(?://.*)?$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        [Fact]
        public void ToDtoAssignsEveryMemberOfThingDefDto()
        {
            string source = ItemsSource();
            string classBody = Block(source, "class ThingDefDto");
            string toDto = Block(source, "public static ThingDefDto ToDto");

            List<string> members = PropertyDeclaration.Matches(classBody).Cast<Match>()
                .Concat(FieldDeclaration.Matches(classBody).Cast<Match>())
                .Select(match => match.Groups["name"].Value)
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            // A scan that finds nothing passes the check below for free.
            Assert.True(
                members.Count >= 30,
                $"only {members.Count} members found; the scan is reading the wrong block");
            Assert.Contains("ArmorRatingSharp", members);

            List<string> unassigned = members
                .Where(name => !Regex.IsMatch(
                    toDto, @"^\s*" + Regex.Escape(name) + @"\s*=", RegexOptions.Multiline))
                .ToList();

            Assert.True(
                unassigned.Count == 0,
                "ThingDefDto.ToDto leaves these members unset, so every answer reports their default: "
                    + string.Join(", ", unassigned));
        }

        private static string ItemsSource() => File.ReadAllText(Path.Combine(
            SourceTree.RepoRoot(),
            "Source", "RIMAPI", "RimworldRestApi", "Models", "Items", "ItemsDto.cs"));

        /// <summary>
        /// The braced body that follows a declaration, so the member list comes
        /// from its own class rather than from the next one in the same file.
        /// </summary>
        private static string Block(string source, string declaration)
        {
            int at = source.IndexOf(declaration, StringComparison.Ordinal);
            if (at < 0)
                throw new InvalidOperationException("no '" + declaration + "' in ItemsDto.cs");

            int open = source.IndexOf('{', at);
            if (open < 0)
                throw new InvalidOperationException("no body after '" + declaration + "'");

            int depth = 0;
            for (int i = open; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                }
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return source.Substring(open + 1, i - open - 1);
                }
            }

            throw new InvalidOperationException("unbalanced braces after '" + declaration + "'");
        }
    }
}
