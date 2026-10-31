using System.IO;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The archive route's own filter (ticket #3428). It runs before the limit,
    /// so what it drops decides whether `limit` entries are entries a report
    /// can print. The rule mirrors the fox's Go read (MessageNoise in
    /// core/rimworld/messages.go); a case added on one side alone shows up as a
    /// line the report did not want.
    /// </summary>
    public class ArchiveNoiseTests
    {
        [Fact]
        public void TheGamesOwnSaveNoticeIsNoise()
        {
            // SilentInput is what the bridge's own scratch save writes, and the
            // text check catches a save notice under any other def.
            Assert.True(ArchiveNoise.IsNoise("SilentInput", "Game saved as: fox-snapshot"));
            Assert.True(ArchiveNoise.IsNoise("SomeOtherDef", "Game saved as: Asbutora7"));
        }

        [Fact]
        public void TheAlertTextAnotherReadCarriesIsNoise()
        {
            Assert.True(ArchiveNoise.IsNoise("ThreatBig", "Critical alert: Major break risk"));
        }

        [Fact]
        public void TheIdleNotesAreNoise()
        {
            Assert.True(ArchiveNoise.IsNoise("PositiveEvent", "2 colonists are idle"));
            Assert.True(ArchiveNoise.IsNoise("PositiveEvent", "Colonist is idle"));
        }

        [Fact]
        public void AnEntryWithoutTextIsNoise()
        {
            Assert.True(ArchiveNoise.IsNoise("NegativeEvent", ""));
            Assert.True(ArchiveNoise.IsNoise("NegativeEvent", null));
        }

        [Fact]
        public void ARealNotificationStays()
        {
            Assert.False(ArchiveNoise.IsNoise("NegativeEvent", "Rice plant has died because of cold."));
            Assert.False(ArchiveNoise.IsNoise("ThreatBig", "Maskinnen is being attacked by Haplo!"));
        }

        [Fact]
        public void ACapitalisedTextIsRecognisedToo()
        {
            // The game's own texts are translated; the prefix match must not
            // depend on the caller's casing.
            Assert.True(ArchiveNoise.IsNoise("", "  CRITICAL ALERT: Raid"));
        }

        [Fact]
        public void TheFilterRunsBeforeTheLimit()
        {
            // The ticket's point: dropping the noise after the route trimmed
            // would answer fewer real entries than asked for.
            string path = Path.Combine(
                SourceTree.RepoRoot(),
                "Source", "RIMAPI", "RimworldRestApi", "Services", "Client",
                "MessageService", "MessageService.cs");
            string source = File.ReadAllText(path);

            int filter = source.IndexOf("ArchiveNoise.IsNoise");
            int trim = source.IndexOf("entries.Skip(");
            Assert.True(filter >= 0, "ListMessages must drop the noise");
            Assert.True(trim >= 0, "ListMessages must still cap the answer");
            Assert.True(filter < trim, "the noise must be dropped before the limit applies");
        }
    }
}
