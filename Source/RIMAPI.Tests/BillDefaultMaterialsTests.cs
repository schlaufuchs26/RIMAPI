using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// A bill read answers three filter sets, and the default is not the
    /// recipe's whole set (fox ticket #4027). The game copies
    /// RecipeDef.defaultIngredientFilter into a new bill (Bill..ctor) and only
    /// fills that in from fixedIngredientFilter when the recipe names none, so
    /// a butcher bill starts with its 115 animal corpses while the recipe
    /// offers 127. Reporting the fixed filter as the default made
    /// "restore the default" widen a filter. The DTO fields are filled inside
    /// BillHelper.ToDto, which needs the running game, so this check reads the
    /// source: the field must come from the default filter.
    /// </summary>
    public class BillDefaultMaterialsTests
    {
        private static string Helper() =>
            File.ReadAllText(Path.Combine(
                SourceTree.RepoRoot(), "Source", "RIMAPI", "RimworldRestApi", "Helpers", "BillHelper.cs"));

        private static string Dto() =>
            File.ReadAllText(Path.Combine(
                SourceTree.RepoRoot(), "Source", "RIMAPI", "RimworldRestApi", "Models", "BillDto.cs"));

        [Fact]
        public void TheDefaultMaterialsComeFromTheRecipeDefaultFilter()
        {
            Match field = Regex.Match(
                Helper(), @"DefaultMaterials\s*=\s*(?<source>[^,\n]+)", RegexOptions.Compiled);

            Assert.True(field.Success, "BillHelper no longer fills DefaultMaterials");
            Assert.Contains("defaultIngredientFilter", field.Groups["source"].Value);
            Assert.DoesNotContain("fixedIngredientFilter", field.Groups["source"].Value);
        }

        [Fact]
        public void TheAvailableMaterialsStayTheRecipesWholeSet()
        {
            Match field = Regex.Match(
                Helper(), @"AvailableMaterials\s*=\s*(?<source>[^,\n]+)", RegexOptions.Compiled);

            Assert.True(field.Success, "BillHelper no longer fills AvailableMaterials");
            Assert.Contains("fixedIngredientFilter", field.Groups["source"].Value);
        }

        [Fact]
        public void TheBillDtoCarriesTheDefaultList()
        {
            Assert.Matches(@"public List<string> DefaultMaterials", Dto());
        }
    }
}
