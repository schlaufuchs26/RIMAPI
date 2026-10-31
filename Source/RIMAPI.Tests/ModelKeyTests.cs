using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RIMAPI.Core;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The snake_case contract over the whole Models tree (ticket #3377).
    /// #3316 pinned the keys of the few DTOs that compile against the stubs;
    /// the other ~280 DTO files in Models/ could rename a member and no test
    /// would notice, because the caller only ever sees the JSON key. Reading
    /// the sources covers all of them without a stub per file.
    /// </summary>
    public class ModelKeyTests
    {
        // A public property or field declaration, minus the members that never
        // reach JSON (constants, methods, nested types). The type part excludes
        // the characters a member body would start with, so a method that
        // returns a type does not match.
        private static readonly Regex PropertyDeclaration = new Regex(
            @"^\s*public\s+(?!(?:static|const|class|enum|struct|interface|delegate|event|override|virtual|abstract)\b)"
                + @"[^;{}=()]+?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get",
            RegexOptions.Compiled);

        private static readonly Regex FieldDeclaration = new Regex(
            @"^\s*public\s+(?!(?:static|const|class|enum|struct|interface|delegate|event|override|virtual|abstract)\b)"
                + @"[^;{}=()]+?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:=[^;]*)?;\s*(?://.*)?$",
            RegexOptions.Compiled);

        private static readonly Regex ClassDeclaration = new Regex(
            @"^\s*(?:public|internal)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled);

        // Only an attribute with a name bypasses the resolver; [JsonProperty(
        // NullValueHandling = ...)] changes the value, not the key.
        private static readonly Regex ExplicitKeyAttribute = new Regex(
            @"\[JsonProperty\(\s*""(?<key>[^""]*)""",
            RegexOptions.Compiled);

        private static readonly Regex Acronym = new Regex(@"[A-Z]{2}", RegexOptions.Compiled);

        // The key a member name turns into, as every API answer and request
        // body spells it: lowercase, words separated by one underscore.
        private static readonly Regex ReadableKey =
            new Regex(@"^[a-z][a-z0-9]*(_[a-z0-9]+)*$", RegexOptions.Compiled);

        [Fact]
        public void TheScanReadsTheWholeModelsTree()
        {
            // A scan that finds nothing passes every rule below for free.
            List<ModelMember> members = Members();

            Assert.True(
                members.Count > 1500,
                $"only {members.Count} members found; the scan is reading the wrong tree");
            Assert.Contains(members, m => m.Type == "PositionDto" && m.Name == "Z");
            Assert.Contains(members, m => m.Type == "NativeScreenshotRequestDto" && m.Name == "FileName");
        }

        [Fact]
        public void NoMemberCarriesAnAcronymWithoutAnExplicitKey()
        {
            var probe = new ProbeResolver();
            List<string> offenders = Members()
                .Where(m => m.ExplicitKey == null && Acronym.IsMatch(m.Name))
                .Select(m => $"{m.Where}: {m.Name} -> {probe.Key(m.Name)}")
                .ToList();

            Assert.True(
                offenders.Count == 0,
                "The resolver splits every capital, so an acronym turns into single "
                    + "letters (HP becomes h_p) and a caller that reads hp gets nothing. "
                    + "Give the member an explicit [JsonProperty(\"...\")] naming its key:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, offenders));
        }

        [Fact]
        public void NoMemberKeyCarriesALetterFragmentBeforeItsLastWord()
        {
            // The same acronym problem, seen on the key: a single-letter segment
            // anywhere but last is a fragment of a split acronym (hide_u_i). A
            // trailing one is a coordinate or a unit (point_a, bed_room_temp_c)
            // and is what the callers already read.
            var probe = new ProbeResolver();
            List<string> offenders = Members()
                .Where(m => m.ExplicitKey == null)
                .Select(m => new { m, key = probe.Key(m.Name) })
                .Where(x =>
                {
                    string[] words = x.key.Split('_');
                    return words.Length > 1 && words.Take(words.Length - 1).Any(w => w.Length == 1);
                })
                .Select(x => $"{x.m.Where}: {x.m.Name} -> {x.key}")
                .ToList();

            Assert.True(
                offenders.Count == 0,
                "A key with a one-letter word in the middle is a split acronym, not a "
                    + "name a caller would guess; rename the member or give it an "
                    + "explicit [JsonProperty(\"...\")]:" + Environment.NewLine
                    + string.Join(Environment.NewLine, offenders));
        }

        [Fact]
        public void EveryMemberKeyIsLowercaseSnakeCase()
        {
            var probe = new ProbeResolver();
            List<string> offenders = Members()
                .Where(m => m.ExplicitKey == null)
                .Select(m => new { m, key = probe.Key(m.Name) })
                .Where(x => !ReadableKey.IsMatch(x.key))
                .Select(x => $"{x.m.Where}: {x.m.Name} -> {x.key}")
                .ToList();

            Assert.True(
                offenders.Count == 0,
                "Every key is lowercase snake_case with single underscores; a member "
                    + "that breaks that reads as a different field to the caller:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, offenders));
        }

        [Fact]
        public void EveryExplicitKeyIsSnakeCase()
        {
            // An explicit name is written by hand, so nothing else checks it.
            List<string> offenders = Members()
                .Where(m => m.ExplicitKey != null && !ReadableKey.IsMatch(m.ExplicitKey))
                .Select(m => $"{m.Where}: {m.Name} -> {m.ExplicitKey}")
                .ToList();

            Assert.True(
                offenders.Count == 0,
                "An explicit [JsonProperty] name bypasses the resolver, so it has to "
                    + "be spelled the way the resolver would spell it:" + Environment.NewLine
                    + string.Join(Environment.NewLine, offenders));
        }

        [Fact]
        public void TheExplicitKeysAreTheOnesTheToolReads()
        {
            // These are the only keys written by hand; everything else follows
            // its member name. Adding one, renaming one or dropping the attribute
            // has to update this list, which is the point: the caller cannot see
            // the difference until it reads an empty field.
            var expected = new[]
            {
                "CameraScreenshotRequestDto.Format -> format",
                "CameraScreenshotRequestDto.Height -> height",
                "CameraScreenshotRequestDto.HideUI -> hide_ui",
                "CameraScreenshotRequestDto.Quality -> quality",
                "CameraScreenshotRequestDto.Width -> width",
                "ColonistsWorkPrioritiesRequestDto.Priorities -> priorities",
                "NativeScreenshotRequestDto.HideUI -> hide_ui",
                "WorkPriorityRequestDto.Id -> id",
                "WorkPriorityRequestDto.Priority -> priority",
                "WorkPriorityRequestDto.Work -> work",
            };

            List<string> actual = Members()
                .Where(m => m.ExplicitKey != null)
                .Select(m => $"{m.Type}.{m.Name} -> {m.ExplicitKey}")
                .OrderBy(line => line, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(
                expected.OrderBy(line => line, StringComparer.Ordinal),
                actual);
        }

        /// <summary>
        /// Reads every member declaration in the Models tree. Attributes sit on
        /// the line above the member, so they are collected until a declaration
        /// consumes them.
        /// </summary>
        private static List<ModelMember> Members()
        {
            string root = SourceTree.RepoRoot();
            var members = new List<ModelMember>();

            foreach (string file in SourceTree.Sources(root, "RimworldRestApi", "Models"))
            {
                string type = null;
                var attributes = new List<string>();
                int number = 0;

                foreach (string line in File.ReadLines(file))
                {
                    number++;

                    Match classMatch = ClassDeclaration.Match(line);
                    if (classMatch.Success)
                        type = classMatch.Groups["name"].Value;

                    Match member = PropertyDeclaration.Match(line);
                    if (!member.Success)
                        member = FieldDeclaration.Match(line);

                    if (member.Success)
                    {
                        members.Add(new ModelMember
                        {
                            Type = type,
                            Name = member.Groups["name"].Value,
                            ExplicitKey = attributes
                                .Select(attribute => ExplicitKeyAttribute.Match(attribute))
                                .Where(match => match.Success)
                                .Select(match => match.Groups["key"].Value)
                                .FirstOrDefault(),
                            Where = SourceTree.Relative(root, file) + ":" + number,
                        });
                        attributes.Clear();
                        continue;
                    }

                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("["))
                        attributes.Add(trimmed);
                    else if (trimmed.Length > 0 && !trimmed.StartsWith("//"))
                        attributes.Clear();
                }
            }

            return members;
        }

        private sealed class ModelMember
        {
            public string Type { get; set; }

            public string Name { get; set; }

            /// <summary>The name from [JsonProperty("...")], or null.</summary>
            public string ExplicitKey { get; set; }

            /// <summary>File and line, for a message that points at the member.</summary>
            public string Where { get; set; }
        }

        /// <summary>
        /// Reaches the protected name mapping, so a test can ask for one key
        /// without building a DTO around it.
        /// </summary>
        private sealed class ProbeResolver : SnakeCaseContractResolver
        {
            public string Key(string memberName) => ResolvePropertyName(memberName);
        }
    }
}
