using System.IO;
using System.Linq;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The outfits read names the pawns of each policy (fox ticket #4232). The
    /// Assign tab shows both, and the write's answer already counted them; a
    /// caller that wanted "who wears the tattered policy" read every colonist.
    ///
    /// Who wears a policy is one query, so the count and the list cannot
    /// disagree. These checks read the two call sites, because the query itself
    /// needs a loaded colony and no test has one.
    /// </summary>
    public class OutfitReadPawnTests
    {
        private static readonly string[] Files =
        {
            Path.Combine("RimworldRestApi", "Helpers", "PawnHelper.cs"),
            Path.Combine(
                "RimworldRestApi",
                "Services",
                "Pawns",
                "OutfitService",
                "OutfitService.cs"
            ),
        };

        [Fact]
        public void TheOutfitsReadBuildsEachPolicysPawnsFromTheSharedQuery()
        {
            var read = Methods("GetOutfits");

            Assert.Contains("PawnsWearing(policy)", SourceTree.Squash(read.Body));
            Assert.Contains("PawnId=", SourceTree.Squash(read.Body));
        }

        [Fact]
        public void TheWritesCountUsesTheSameQueryAsTheRead()
        {
            var readBack = Methods("ReadBack");

            Assert.Contains("PawnHelper.PawnsWearing(policy)", SourceTree.Squash(readBack.Body));
        }

        [Fact]
        public void TheQueryIsTheGamesOwnPawnListFilteredByThePolicy()
        {
            var query = Methods("PawnsWearing");

            Assert.Contains(
                "p.outfits.CurrentApparelPolicy==policy",
                SourceTree.Squash(query.Body)
            );
        }

        private static SourceMethod Methods(string name)
        {
            string root = SourceTree.RepoRoot();
            string[] paths = Files
                .Select(f => Path.Combine(root, "Source", "RIMAPI", f))
                .ToArray();

            return SourceTree.Methods(paths, root).Single(m => m.Name == name);
        }
    }
}
