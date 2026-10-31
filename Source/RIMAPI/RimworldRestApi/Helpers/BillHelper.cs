using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    public static class BillHelper
    {
        private static readonly FieldInfo LoadIdField = typeof(Bill).GetField("loadID", BindingFlags.Instance | BindingFlags.NonPublic);

        public static BillDto ToDto(Bill_Production bill)
        {
            if (bill == null)
                return null;

            try
            {
                if (LoadIdField == null)
                    Core.LogApi.Warning("[BillHelper] Failed to access loadID field via reflection");

                int loadId = LoadIdField != null ? (int)LoadIdField.GetValue(bill) : 0;

                var dto = new BillDto
                {
                    LoadId = loadId,
                    RecipeDefName = bill.recipe.defName,
                    RecipeLabel = bill.recipe.label,
                    Suspended = bill.suspended,
                    Paused = bill.paused,
                    RepeatMode = bill.repeatMode.defName,
                    RepeatCount = bill.repeatCount,
                    TargetCount = bill.targetCount,
                    StoreMode = bill.GetStoreMode().defName,
                    PauseWhenSatisfied = bill.pauseWhenSatisfied,
                    UnpauseWhenYouHave = bill.unpauseWhenYouHave,
                    IncludeEquipped = bill.includeEquipped,
                    IncludeTainted = bill.includeTainted,
                    HpRange = new Models.FloatRange(bill.hpRange.min, bill.hpRange.max),
                    QualityRange = new IntRangeDto((int)bill.qualityRange.min, (int)bill.qualityRange.max),
                    LimitToAllowedStuff = bill.limitToAllowedStuff,
                    IngredientSearchRadius = bill.ingredientSearchRadius,
                    AllowedSkillRange = new IntRangeDto(bill.allowedSkillRange.min, bill.allowedSkillRange.max),
                    PawnRestrictionId = bill.PawnRestriction?.thingIDNumber,
#if RIMWORLD_1_5
                    PlayerCustomName = "Not supported",
#elif RIMWORLD_1_6
                    PlayerCustomName = bill.RenamableLabel,
#endif
                    SlotGroupId = null,
                    AllowedMaterials = bill.ingredientFilter?.AllowedThingDefs?.Select(d => d.defName).ToList() ?? new List<string>(),
                    AvailableMaterials = bill.recipe.fixedIngredientFilter?.AllowedThingDefs?.Select(d => d.defName).ToList() ?? new List<string>(),
                };
                ApplyState(dto, bill);
                return dto;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"[BillHelper] Error converting Bill to DTO: {ex}");
                return null;
            }
        }

        // ApplyState fills the state fields (ticket #3306) from the game's own
        // answers, so a caller learns why a bill does not run without reading
        // the whole colony. Bill_Production carries no public "satisfied" in
        // 1.6, so the target count is the resource counter's count, plus the
        // worn apparel when the bill counts equipped gear.
        private static void ApplyState(BillDto dto, Bill_Production bill)
        {
            Map map = bill.Map;
            if (map == null)
                return;

            var recipe = bill.recipe;
            ThingDef product = recipe.ProducedThingDef;
            int have = product == null ? 0 : CountInColony(bill, product, map);
            dto.ProductsInColony = have;
            dto.MissingIngredients = MissingIngredients(recipe, map);

            var facts = new BillStateReason.Facts
            {
                Suspended = bill.suspended,
                Paused = bill.paused,
                TargetMet = bill.repeatMode == BillRepeatModeDefOf.TargetCount
                    && product != null
                    && have >= bill.targetCount,
                TargetText = TargetText(bill, product, have),
                MissingIngredients = dto.MissingIngredients,
            };

            if (recipe.skillRequirements != null && recipe.skillRequirements.Count > 0)
            {
                SkillRequirement requirement = recipe.skillRequirements[0];
                facts.Skill = requirement.skill?.label;
                facts.MinSkill = requirement.minLevel;
                facts.BestSkill = 0;
                foreach (Pawn pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn.skills == null || pawn.skills.GetSkill(requirement.skill) == null)
                        continue;
                    int level = pawn.skills.GetSkill(requirement.skill).Level;
                    if (level > facts.BestSkill)
                        facts.BestSkill = level;
                    if (recipe.PawnSatisfiesSkillRequirements(pawn))
                        facts.SkilledColonists++;
                }
                dto.WorkSkill = facts.Skill;
                dto.MinSkill = facts.MinSkill;
                dto.SkilledColonists = facts.SkilledColonists;
            }

            BillStateReason.State state = BillStateReason.Describe(facts);
            dto.State = state.Name;
            dto.StateReason = state.Reason;
        }

        // CountInColony is the number a "do until you have X" target is
        // compared against: what the colony owns, plus what the colonists wear
        // when the bill counts equipped gear (the game's includeEquipped).
        private static int CountInColony(Bill_Production bill, ThingDef product, Map map)
        {
            int have = map.resourceCounter.GetCount(product);
            if (!bill.includeEquipped)
                return have;
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.apparel == null)
                    continue;
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    if (apparel.def == product)
                        have++;
                }
            }
            return have;
        }

        // TargetText says how far a "do until you have X" bill is. Other
        // repeat modes have no target, so they get none.
        private static string TargetText(Bill_Production bill, ThingDef product, int have)
        {
            if (bill.repeatMode != BillRepeatModeDefOf.TargetCount || product == null)
                return null;
            return string.Format("{0} of {1} {2} in the colony", have, bill.targetCount, product.label);
        }

        // MissingIngredients names the ingredients the colony has none of at
        // all, in the recipe's own labels. The game's own helper answers the
        // defs, so a filter that no stored thing matches counts as missing.
        private static List<string> MissingIngredients(RecipeDef recipe, Map map)
        {
            var missing = new List<string>();
            foreach (ThingDef def in recipe.PotentiallyMissingIngredients(null, map))
            {
                if (def != null)
                    missing.Add(def.label);
            }
            return missing;
        }

        public static RecipeDto ToRecipeDto(RecipeDef recipe)
        {
            if (recipe == null)
                return null;

            var dto = new RecipeDto
            {
                DefName = recipe.defName,
                Label = recipe.label,
                Description = recipe.description,
                WorkAmount = recipe.workAmount,
                WorkSkill = recipe.workSkill?.label,
            };

            if (recipe.products != null)
            {
                dto.Products = recipe.products
                    .Select(p => new RecipeProductDto
                    {
                        ThingDef = p.thingDef.defName,
                        Count = p.count,
                    })
                    .ToList();
            }

            if (recipe.ingredients != null)
            {
                dto.Ingredients = recipe.ingredients
                    .Select(i => new BillRecipeIngredientDto
                    {
                        FilterLabel = i.filter.Summary,
                        Count = i.GetBaseCount(),
                    })
                    .ToList();
            }

            return dto;
        }

        public static WorkTableDto ToWorkTableDto(Building_WorkTable wt)
        {
            if (wt == null)
                return null;

            return new WorkTableDto
            {
                Id = wt.thingIDNumber,
                ThingDef = wt.def.defName,
                Label = wt.LabelShort,
                Position = new PositionDto
                {
                    X = wt.Position.x,
                    Y = wt.Position.y,
                    Z = wt.Position.z,
                },
                BillsCount = wt.BillStack.Bills.Count,
            };
        }
    }
}
