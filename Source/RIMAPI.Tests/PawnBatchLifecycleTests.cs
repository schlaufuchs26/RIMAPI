using System;
using System.IO;
using Xunit;

namespace RIMAPI.Tests
{
    // A candidate batch belongs to the world it was drawn in (fox ticket
    // #3924). Founding a colony replaces that world, so the batch has to be
    // dropped there too, not only on a load or a return to the main menu
    // (ticket #4020): otherwise the next read names a ready batch whose seed
    // points at a world that no longer exists.
    public class PawnBatchLifecycleTests
    {
        [Theory]
        [InlineData("GameNewPatch")]
        public void FoundingDropsTheCandidateBatch(string patchClass)
        {
            string path = Path.Combine(
                SourceTree.RepoRoot(),
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Hooks",
                "GameStateHooks.cs"
            );
            string patch = PatchBody(File.ReadAllText(path), patchClass);

            Assert.True(
                patch.Contains("PawnBatch.Reset()"),
                patchClass + " founds a colony but keeps the batch drawn in the setup's world"
            );
        }

        [Theory]
        [InlineData("GameStateService.cs", "GameLoad", "PawnBatch.Reset()")]
        [InlineData("GameStateService.cs", "GoToMainMenu", "PawnBatch.Reset()")]
        public void LoadAndMenuStillDropTheBatch(string file, string method, string call)
        {
            // The two paths that already cleared it stay: the founding patch
            // adds a third, it does not replace them.
            string path = Path.Combine(
                SourceTree.RepoRoot(),
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "System",
                "GameStateService",
                file
            );
            string text = File.ReadAllText(path);
            int start = text.IndexOf(" " + method + "(", StringComparison.Ordinal);
            Assert.True(start >= 0, "no method " + method + " in " + file);
            int next = text.IndexOf("public ApiResult", start + 1, StringComparison.Ordinal);
            string body = next < 0 ? text.Substring(start) : text.Substring(start, next - start);

            Assert.Contains(call, body);
        }

        /// <summary>The text of one Harmony patch class, up to the next patch.</summary>
        private static string PatchBody(string text, string patchClass)
        {
            int start = text.IndexOf("class " + patchClass, StringComparison.Ordinal);
            Assert.True(start >= 0, "no patch class " + patchClass + " in GameStateHooks.cs");
            int next = text.IndexOf("[HarmonyPatch(", start, StringComparison.Ordinal);
            return next < 0 ? text.Substring(start) : text.Substring(start, next - start);
        }
    }
}
