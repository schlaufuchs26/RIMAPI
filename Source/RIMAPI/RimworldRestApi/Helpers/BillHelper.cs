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
                    PlayerCustomName = bill.RenamableLabel,
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
                PowerFault = BenchPowerFault(bill),
            };

            List<WorkTypeDef> workTypes = BenchWorkTypes(bill);
            if (workTypes.Count > 0)
            {
                dto.WorkTypes = workTypes.Select(w => w.defName).ToList();
                facts.WorkTypes = dto.WorkTypes;
                foreach (Pawn pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn.workSettings == null)
                        continue;
                    if (workTypes.Any(w => pawn.workSettings.WorkIsActive(w)))
                        facts.AssignedColonists++;
                }
            }

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

            // The recipe's work skill is what a slow worker shows up in, and it
            // is not the same thing as a skill requirement: a simple meal needs
            // no level at all, which is exactly the case a colony of level-0
            // cooks starves in (ticket #3371).
            ApplyBestSkill(dto, recipe, map, workTypes);
        }

        // BenchPowerFault answers why the bench cannot run for want of power
        // (fox ticket #3762), or null when it can. The bill's own state said
        // "running" for four game hours on a stove that stood four cells from
        // the nearest conduit and had no net at all, while the meals fell from
        // 9 to 4.
        private static string BenchPowerFault(Bill_Production bill)
        {
            Thing bench = bill.billStack?.billGiver as Thing;
            if (bench == null)
                return null;

            CompPowerTrader power = bench.TryGetComp<CompPowerTrader>();
            if (power == null || power.Props.PowerConsumption <= 0f)
                return null;

            var facts = new PowerFault.Facts
            {
                Label = bench.Label,
                ConsumesPower = true,
                SwitchedOn = FlickUtility.WantsToBeOn(bench),
                Connected = power.PowerNet != null,
                Powered = power.PowerOn,
                ConduitDistance = -1,
            };
            if (power.PowerNet != null)
            {
                facts.NetProduction = power.PowerNet.CurrentEnergyGainRate();
                facts.NetStored = power.PowerNet.CurrentStoredEnergy();
            }

            Building building = bench as Building;
            if (building != null && !facts.Connected)
            {
                IntVec3 cell;
                facts.ConduitDistance = PowerGrid.NearestConduit(building, out cell);
                if (cell.IsValid)
                {
                    facts.ConduitX = cell.x;
                    facts.ConduitZ = cell.z;
                }
            }
            return PowerFault.Describe(facts);
        }

        // ApplyBestSkill names the colonist a caller would expect to do the
        // work, with his level in the recipe's own work skill. Empty when the
        // recipe has no work skill (nothing to be slow at) or no colonist has
        // that skill record.
        private static void ApplyBestSkill(BillDto dto, RecipeDef recipe, Map map, List<WorkTypeDef> workTypes)
        {
            SkillDef workSkill = recipe.workSkill;
            if (workSkill == null)
                return;
            if (string.IsNullOrEmpty(dto.WorkSkill))
                dto.WorkSkill = workSkill.label;

            var candidates = new List<SkillPicker.Candidate>();
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                SkillRecord record = pawn.skills?.GetSkill(workSkill);
                if (record == null)
                    continue;
                bool works = workTypes.Count == 0;
                if (!works && pawn.workSettings != null)
                    works = workTypes.Any(w => pawn.workSettings.WorkIsActive(w));
                candidates.Add(new SkillPicker.Candidate
                {
                    Name = pawn.LabelShort ?? pawn.Name?.ToStringShort,
                    Level = record.Level,
                    Works = works,
                });
            }
            SkillPicker.Best best = SkillPicker.Pick(candidates);
            dto.BestSkill = best.Level;
            dto.BestSkillPawn = best.Name;
        }

        // BenchWorkTypes is the work type (or types) that can run this bench,
        // read from the work givers that name the bench's def: a stove is
        // Cooking, a machining table Smithing, a drug lab Crafting (ticket
        // #3362). Empty when the game names no giver for the def, and then
        // nothing is claimed about the assignment.
        private static List<WorkTypeDef> BenchWorkTypes(Bill_Production bill)
        {
            var types = new List<WorkTypeDef>();
            Thing bench = bill.billStack?.billGiver as Thing;
            if (bench == null)
                return types;
            foreach (WorkGiverDef workGiver in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (workGiver.workType == null || workGiver.fixedBillGiverDefs == null)
                    continue;
                if (!workGiver.fixedBillGiverDefs.Contains(bench.def))
                    continue;
                if (!types.Contains(workGiver.workType))
                    types.Add(workGiver.workType);
            }
            return types;
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
