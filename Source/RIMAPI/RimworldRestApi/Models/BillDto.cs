using System.Collections.Generic;

namespace RIMAPI.Models
{
    public class IntRangeDto
    {
        public int Min { get; set; }
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
        public int LoadId { get; set; }
        public string RecipeDefName { get; set; }
        public string RecipeLabel { get; set; }
        public bool Suspended { get; set; }
        public bool Paused { get; set; }
        public string RepeatMode { get; set; }
        public int RepeatCount { get; set; }
        public int TargetCount { get; set; }
        public string StoreMode { get; set; }
        public bool PauseWhenSatisfied { get; set; }
        public int UnpauseWhenYouHave { get; set; }
        public bool IncludeEquipped { get; set; }
        public bool IncludeTainted { get; set; }
        public FloatRange HpRange { get; set; }
        public IntRangeDto QualityRange { get; set; }
        public bool LimitToAllowedStuff { get; set; }
        public float IngredientSearchRadius { get; set; }
        public IntRangeDto AllowedSkillRange { get; set; }
        public int? PawnRestrictionId { get; set; }
        public string PlayerCustomName { get; set; }
        public int? SlotGroupId { get; set; }
        public List<string> AllowedMaterials { get; set; } = new List<string>();
        public List<string> AvailableMaterials { get; set; } = new List<string>();
        // What the bill is doing, in the game's own terms (ticket #3306):
        // State is one of running, suspended, paused, target met, missing
        // ingredients, no colonist assigned or no worker with the skill, and
        // StateReason says what to fix. ProductsInColony is the count the
        // target is compared against, MissingIngredients the ingredients the
        // colony has none of, and the skill fields carry the recipe's
        // requirement next to how many colonists meet it. WorkTypes are the
        // defNames of the bench's work types (ticket #3362), the ones
        // `work_priority` takes.
        public string State { get; set; }
        public string StateReason { get; set; }
        public int ProductsInColony { get; set; }
        public List<string> MissingIngredients { get; set; } = new List<string>();
        public string WorkSkill { get; set; }
        public int MinSkill { get; set; }
        public int SkilledColonists { get; set; }
        // BestSkill is the highest level any free colonist has in the recipe's
        // work skill (Cooking for a meal bill), and BestSkillPawn the colonist
        // holding it, among those whose work settings include the bench's work
        // type. A cook bill that runs while the colony has no meals is either a
        // broken kitchen or a slow cook, and "the best cook has Cooking 0" is
        // the number that tells them apart (ticket #3371).
        public int BestSkill { get; set; }
        public string BestSkillPawn { get; set; }
        public List<string> WorkTypes { get; set; } = new List<string>();
    }

    public class CreateBillRequest
    {
        public string RecipeDefName { get; set; }
        public string RepeatMode { get; set; }
        public int? RepeatCount { get; set; }
        public int? TargetCount { get; set; }
        public string StoreMode { get; set; }
        public bool? Suspended { get; set; }
        public bool? PauseWhenSatisfied { get; set; }
        public int? UnpauseWhenYouHave { get; set; }
        public bool? IncludeEquipped { get; set; }
        public bool? IncludeTainted { get; set; }
        public FloatRange HpRange { get; set; }
        public IntRangeDto QualityRange { get; set; }
        public bool? LimitToAllowedStuff { get; set; }
        public float? IngredientSearchRadius { get; set; }
        public IntRangeDto AllowedSkillRange { get; set; }
        public int? PawnRestrictionId { get; set; }
        public string PlayerCustomName { get; set; }
        public List<string> AllowedMaterials { get; set; }
    }

    public class UpdateBillRequest
    {
        public string RepeatMode { get; set; }
        public int? RepeatCount { get; set; }
        public int? TargetCount { get; set; }
        public string StoreMode { get; set; }
        public bool? Suspended { get; set; }
        public bool? PauseWhenSatisfied { get; set; }
        public int? UnpauseWhenYouHave { get; set; }
        public bool? IncludeEquipped { get; set; }
        public bool? IncludeTainted { get; set; }
        public FloatRange HpRange { get; set; }
        public IntRangeDto QualityRange { get; set; }
        public bool? LimitToAllowedStuff { get; set; }
        public float? IngredientSearchRadius { get; set; }
        public IntRangeDto AllowedSkillRange { get; set; }
        public int? PawnRestrictionId { get; set; }
        public string PlayerCustomName { get; set; }
        public List<string> AllowedMaterials { get; set; }
    }

    public class BillReorderRequest
    {
        public int Offset { get; set; }
    }

    public class BillSuspendRequest
    {
        public bool Suspended { get; set; }
    }

    public class RecipeProductDto
    {
        public string ThingDef { get; set; }
        public int Count { get; set; }
    }

    public class BillRecipeIngredientDto
    {
        public string FilterLabel { get; set; }
        public float Count { get; set; }
    }

    public class RecipeDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public float WorkAmount { get; set; }
        public string WorkSkill { get; set; }
        public List<RecipeProductDto> Products { get; set; } = new List<RecipeProductDto>();
        public List<BillRecipeIngredientDto> Ingredients { get; set; } = new List<BillRecipeIngredientDto>();
    }

    public class WorkTableDto
    {
        public int Id { get; set; }
        public string ThingDef { get; set; }
        public string Label { get; set; }
        public PositionDto Position { get; set; }
        public int BillsCount { get; set; }
    }
}
