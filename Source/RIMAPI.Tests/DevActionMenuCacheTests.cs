using System;
using System.IO;
using Xunit;

namespace RIMAPI.Tests
{
    // The menu cache must cover the whole walk and be dropped on every game
    // change (fox ticket #3946): a load that leaves it in place would answer
    // the next colony with the previous one's menu.
    public class DevActionMenuCacheTests
    {
        [Fact]
        public void TheMenuIsBuiltOnceBehindTheCache()
        {
            string path = Path.Combine(
                SourceTree.RepoRoot(),
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "System",
                "DevActionService",
                "DevActionMenu.cs"
            );
            string text = File.ReadAllText(path);

            int builds = Count(text, "new DebugTabMenu_Actions(");

            Assert.True(
                builds == 1,
                path + " builds the menu " + builds + " times; the walk must sit behind CachedMenu.Get"
            );
            Assert.Contains("CachedMenu.Get(", text);
        }

        [Theory]
        [InlineData("GameLoadPatch", "game_loaded")]
        [InlineData("GameNewPatch", "new_game_created")]
        [InlineData("ExitToMenuPatch", "exit_to_menu")]
        public void EveryGameChangeDropsTheMenu(string patchClass, string stateEvent)
        {
            string root = SourceTree.RepoRoot();
            string path = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Hooks",
                "GameStateHooks.cs"
            );
            string text = File.ReadAllText(path);
            string patch = PatchBody(text, patchClass);

            Assert.Contains("\"" + stateEvent + "\"", patch);
            Assert.True(
                patch.Contains("DevActionService.ForgetActionMenu()"),
                patchClass + " reports " + stateEvent + " but keeps the cached dev-action menu"
            );
        }

        /// <summary>The text of one Harmony patch class, up to the next patch.</summary>
        private static string PatchBody(string text, string patchClass)
        {
            int start = text.IndexOf("class " + patchClass, StringComparison.Ordinal);
            Assert.True(start >= 0, "no patch class " + patchClass + " in GameStateHooks.cs");
            int next = text.IndexOf("[HarmonyPatch(", start, StringComparison.Ordinal);
            return next < 0 ? text.Substring(start) : text.Substring(start, next - start);
        }

        private static int Count(string text, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }
    }
}
