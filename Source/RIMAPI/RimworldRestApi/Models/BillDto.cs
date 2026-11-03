using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class IntRangeDto
    {
        [ParameterDescription("The lowest value the range allows.")]
        public int Min { get; set; }
        [ParameterDescription("The highest value the range allows.")]
        public int Max { get; set; }

        public IntRangeDto() { }

        public IntRangeDto(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    public class BillDto
    {
        [ParameterDescription("The bill's load id, which survives a save.")]
        public int LoadId { get; set; }
        [ParameterDescription("The recipe's def name, e.g. \"Make_Steel\".")]
        public string RecipeDefName { get; set; }
        [ParameterDescription("The recipe's label, as the game shows it.")]
        public string RecipeLabel { get; set; }
        [ParameterDescription("Whether the bill is suspended.")]
        public bool Suspended { get; set; }
        [ParameterDescription("Whether the bill is paused until a condition holds.")]
        public bool Paused { get; set; }
        [ParameterDescription("How the bill repeats: never, until a count, or forever.")]
        public string RepeatMode { get; set; }
        [ParameterDescription("How many times the bill repeats.")]
        public int RepeatCount { get; set; }
        [ParameterDescription("The stock level the bill keeps the colony at.")]
        public int TargetCount { get; set; }
        [ParameterDescription("Where the products go: the best stockpile, or a chosen one.")]
        public string StoreMode { get; set; }
        [ParameterDescription("Pause the bill once the target count is reached.")]
        public bool PauseWhenSatisfied { get; set; }
        [ParameterDescription("Resume the bill when the stock falls below this count.")]
        public int UnpauseWhenYouHave { get; set; }
        [ParameterDescription("Whether equipped items count toward the target.")]
        public bool IncludeEquipped { get; set; }
        [ParameterDescription("Whether tainted items count toward the target.")]
        public bool IncludeTainted { get; set; }
        [ParameterDescription("The hit point range the bill accepts, in percent.")]
        public FloatRange HpRange { get; set; }
        [ParameterDescription("Which item qualities the bill accepts, from Awful up to Legendary.")]
        public IntRangeDto QualityRange { get; set; }
        [ParameterDescription("Whether only the work table's allowed materials pass.")]
        public bool LimitToAllowedStuff { get; set; }
        [ParameterDescription("How far the colony hauls ingredients for the bill, in cells.")]
        public float IngredientSearchRadius { get; set; }
        [ParameterDescription("The skill range the bill's crafter must fall in.")]
        public IntRangeDto AllowedSkillRange { get; set; }
        [ParameterDescription("The numeric id of the only pawn allowed to do the bill.")]
        public int? PawnRestrictionId { get; set; }
        [ParameterDescription("The name the player gave the bill.")]
        public string PlayerCustomName { get; set; }
        [ParameterDescription("The storage the products are taken to.")]
        public int? SlotGroupId { get; set; }
        [ParameterDescription("The materials the bill may use, by def name.")]
        public List<string> AllowedMaterials { get; set; } = new List<string>();
        // AvailableMaterials is everything the recipe can take
        // (RecipeDef.fixedIngredientFilter), DefaultMaterials what a fresh bill
        // of the recipe starts with (RecipeDef.defaultIngredientFilter, which
        // Bill..ctor copies into a new bill). The two differ: a butcher bill
        // starts with the 115 animal corpses of 127 the recipe offers, because
        // the recipe's default filter names CorpsesAnimal. Reporting the fixed
        // filter as the default made "restore the default" widen a filter
        // instead of restoring it (fox ticket #4027).
        [ParameterDescription("The materials the map currently offers for the bill.")]
        public List<string> AvailableMaterials { get; set; } = new List<string>();
        [ParameterDescription("The materials the game picks when the player names none.")]
        public List<string> DefaultMaterials { get; set; } = new List<string>();
        // HumanlikeMaterials are the defNames among AvailableMaterials whose
        // corpse belonged to a humanlike pawn, which is the game's own test
        // (ThingDefGenerator_Corpses files them under CorpsesHumanlike, and
        // Verse.Corpse.ButcherProducts records ButcheredHuman for them). A
        // butcher bill that takes one costs every colonist "I butchered
        // humanlike -12", so the tool refuses such a filter unless the call
        // names the corpse itself (fox ticket #3978).
        [ParameterDescription("The materials a humanlike corpse recipe may use.")]
        public List<string> HumanlikeMaterials { get; set; } = new List<string>();
        // What the bill is doing, in the game's own terms (ticket #3306):
        // State is one of running, suspended, paused, target met, missing
        // ingredients, no colonist assigned or no worker with the skill, and
        // StateReason says what to fix. ProductsInColony is the count the
        // target is compared against, MissingIngredients the ingredients the
        // colony has none of, and the skill fields carry the recipe's
        // requirement next to how many colonists meet it. WorkTypes are the
        // defNames of the bench's work types (ticket #3362), the ones
        // `work_priority` takes.
        [ParameterDescription("What the bill is doing: running, waiting, suspended or finished.")]
        public string State { get; set; }
        [ParameterDescription("Why the bill is in that state.")]
        public string StateReason { get; set; }
        [ParameterDescription("How many of the product the colony already holds.")]
        public int ProductsInColony { get; set; }
        [ParameterDescription("The ingredients the colony cannot supply right now.")]
        public List<string> MissingIngredients { get; set; } = new List<string>();
        [ParameterDescription("The skill a crafter needs to run the recipe.")]
        public string WorkSkill { get; set; }
        [ParameterDescription("The lowest skill level the bill accepts.")]
        public int MinSkill { get; set; }
        [ParameterDescription("How many colonists reach the bill's skill range.")]
        public int SkilledColonists { get; set; }
        // BestSkill is the highest level any free colonist has in the recipe's
        // work skill (Cooking for a meal bill), and BestSkillPawn the colonist
        // holding it, among those whose work settings include the bench's work
        // type. A cook bill that runs while the colony has no meals is either a
        // broken kitchen or a slow cook, and "the best cook has Cooking 0" is
        // the number that tells them apart (ticket #3371).
        [ParameterDescription("The best skill level any colonist has for it.")]
        public int BestSkill { get; set; }
        [ParameterDescription("The colonist with the best skill for it.")]
        public string BestSkillPawn { get; set; }
        [ParameterDescription("The work types that may run the bill.")]
        public List<string> WorkTypes { get; set; } = new List<string>();
    }

    public class CreateBillRequest
    {
        [ParameterDescription("The recipe's def name, e.g. \"Make_Steel\".")]
        public string RecipeDefName { get; set; }
        [ParameterDescription("How the bill repeats: never, until a count, or forever.")]
        public string RepeatMode { get; set; }
        [ParameterDescription("How many times the bill repeats.")]
        public int? RepeatCount { get; set; }
        [ParameterDescription("The stock level the bill keeps the colony at.")]
        public int? TargetCount { get; set; }
        [ParameterDescription("Where the products go: the best stockpile, or a chosen one.")]
        public string StoreMode { get; set; }
        [ParameterDescription("Whether the new bill starts suspended.")]
        public bool? Suspended { get; set; }
        [ParameterDescription("Pause the bill once the target count is reached.")]
        public bool? PauseWhenSatisfied { get; set; }
        [ParameterDescription("Resume the bill when the stock falls below this count.")]
        public int? UnpauseWhenYouHave { get; set; }
        [ParameterDescription("Whether equipped items count toward the target.")]
        public bool? IncludeEquipped { get; set; }
        [ParameterDescription("Whether tainted items count toward the target.")]
        public bool? IncludeTainted { get; set; }
        [ParameterDescription("The hit point range the bill accepts, in percent.")]
        public FloatRange HpRange { get; set; }
        [ParameterDescription("Which item qualities the bill accepts, from Awful up to Legendary.")]
        public IntRangeDto QualityRange { get; set; }
        [ParameterDescription("Whether only the work table's allowed materials pass.")]
        public bool? LimitToAllowedStuff { get; set; }
        [ParameterDescription("How far the colony hauls ingredients for the bill, in cells.")]
        public float? IngredientSearchRadius { get; set; }
        [ParameterDescription("The skill range the bill's crafter must fall in.")]
        public IntRangeDto AllowedSkillRange { get; set; }
        [ParameterDescription("The numeric id of the only pawn allowed to do the bill.")]
        public int? PawnRestrictionId { get; set; }
        [ParameterDescription("The name to give the bill.")]
        public string PlayerCustomName { get; set; }
        [ParameterDescription("The materials the bill may use, by def name.")]
        public List<string> AllowedMaterials { get; set; }
    }

    public class UpdateBillRequest
    {
        [ParameterDescription("How the bill repeats: never, until a count, or forever.")]
        public string RepeatMode { get; set; }
        [ParameterDescription("How many times the bill repeats.")]
        public int? RepeatCount { get; set; }
        [ParameterDescription("The stock level the bill keeps the colony at.")]
        public int? TargetCount { get; set; }
        [ParameterDescription("Where the products go: the best stockpile, or a chosen one.")]
        public string StoreMode { get; set; }
        [ParameterDescription("Whether the bill is suspended.")]
        public bool? Suspended { get; set; }
        [ParameterDescription("Pause the bill once the target count is reached.")]
        public bool? PauseWhenSatisfied { get; set; }
        [ParameterDescription("Resume the bill when the stock falls below this count.")]
        public int? UnpauseWhenYouHave { get; set; }
        [ParameterDescription("Whether equipped items count toward the target.")]
        public bool? IncludeEquipped { get; set; }
        [ParameterDescription("Whether tainted items count toward the target.")]
        public bool? IncludeTainted { get; set; }
        [ParameterDescription("The hit point range the bill accepts, in percent.")]
        public FloatRange HpRange { get; set; }
        [ParameterDescription("Which item qualities the bill accepts, from Awful up to Legendary.")]
        public IntRangeDto QualityRange { get; set; }
        [ParameterDescription("Whether only the work table's allowed materials pass.")]
        public bool? LimitToAllowedStuff { get; set; }
        [ParameterDescription("How far the colony hauls ingredients for the bill, in cells.")]
        public float? IngredientSearchRadius { get; set; }
        [ParameterDescription("The skill range the bill's crafter must fall in.")]
        public IntRangeDto AllowedSkillRange { get; set; }
        [ParameterDescription("The numeric id of the only pawn allowed to do the bill.")]
        public int? PawnRestrictionId { get; set; }
        [ParameterDescription("The name the player gave the bill.")]
        public string PlayerCustomName { get; set; }
        [ParameterDescription("The materials the bill may use, by def name.")]
        public List<string> AllowedMaterials { get; set; }
    }

    public class BillReorderRequest
    {
        [ParameterDescription("How many places to move the bill; negative moves it up.")]
        public int Offset { get; set; }
    }

    public class BillSuspendRequest
    {
        [ParameterDescription("Whether the bill is suspended.")]
        public bool Suspended { get; set; }
    }

    public class RecipeProductDto
    {
        [ParameterDescription("The def name of the product.")]
        public string ThingDef { get; set; }
        public int Count { get; set; }
    }

    public class BillRecipeIngredientDto
    {
        [ParameterDescription("The ingredient filter's label, as the game shows it.")]
        public string FilterLabel { get; set; }
        public float Count { get; set; }
    }

    public class RecipeDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How much work the recipe takes.")]
        public float WorkAmount { get; set; }
        [ParameterDescription("The skill a crafter needs to run the recipe.")]
        public string WorkSkill { get; set; }
        [ParameterDescription("The things one run of the recipe produces.")]
        public List<RecipeProductDto> Products { get; set; } = new List<RecipeProductDto>();
        [ParameterDescription("The ingredients one run of the recipe takes.")]
        public List<BillRecipeIngredientDto> Ingredients { get; set; } = new List<BillRecipeIngredientDto>();
    }

    public class WorkTableDto
    {
        [ParameterDescription("The work table's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("The work table's def name.")]
        public string ThingDef { get; set; }
        public string Label { get; set; }
        public PositionDto Position { get; set; }
        [ParameterDescription("How many bills the table holds.")]
        public int BillsCount { get; set; }
    }
}
