using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    /// <summary>
    /// The Health tab's operations (fox ticket #4212, gap 4). A scarred colonist
    /// stays scarred today because the mod has no Bill_Medical anywhere: the
    /// colony can read a wound and not schedule the surgery that fixes it.
    ///
    /// Everything here is the game's own health tab:
    /// HealthCardUtility.CreateSurgeryBill is the "Add bill" path (it adds the
    /// bill to the pawn's surgery stack, sets the part and posts the game's own
    /// "nobody has the skill" dialog), and BillStack.Delete is the cross.
    /// </summary>
    public class OperationService : IOperationService
    {
        public ApiResult<PawnOperationsDto> GetOperations(int pawnId)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                    return ApiResult<PawnOperationsDto>.Fail(OperationRules.UnknownPawn(pawnId));
                if (pawn.BillStack == null || pawn.playerSettings == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.NoHealthTracker(pawn.LabelShortCap)
                    );

                return ApiResult<PawnOperationsDto>.Ok(ReadOperations(pawn));
            }
            catch (Exception ex)
            {
                return ApiResult<PawnOperationsDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<List<SurgeryRecipeDto>> GetSurgeryRecipes(int pawnId)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                    return ApiResult<List<SurgeryRecipeDto>>.Fail(
                        OperationRules.UnknownPawn(pawnId)
                    );

                var recipes = DefDatabase<RecipeDef>
                    .AllDefsListForReading.Where(r => r.IsSurgery)
                    .Select(r => new SurgeryRecipeDto
                    {
                        Recipe = r.defName,
                        Label = r.LabelCap,
                        TargetsBodyPart = r.targetsBodyPart,
                        Available = r.AvailableOnNow(pawn),
                        SkillOk = r.PawnSatisfiesSkillRequirements(pawn),
                        BodyParts = r.targetsBodyPart
                            ? r.Worker.GetPartsToApplyOn(pawn, r).Select(p => p.Label).ToList()
                            : new List<string>(),
                    })
                    .OrderBy(r => r.Recipe)
                    .ToList();

                return ApiResult<List<SurgeryRecipeDto>>.Ok(recipes);
            }
            catch (Exception ex)
            {
                return ApiResult<List<SurgeryRecipeDto>>.Fail(ex.ToString());
            }
        }

        public ApiResult<PawnOperationsDto> Add(AddOperationRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Recipe))
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.UnknownRecipe(request?.Recipe ?? "")
                    );

                Pawn pawn = PawnHelper.FindPawnById(request.PawnId);
                if (pawn == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.UnknownPawn(request.PawnId)
                    );
                if (pawn.BillStack == null || pawn.playerSettings == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.NoHealthTracker(pawn.LabelShortCap)
                    );

                RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(request.Recipe.Trim());
                if (recipe == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.UnknownRecipe(request.Recipe)
                    );
                if (!recipe.IsSurgery)
                    return ApiResult<PawnOperationsDto>.Fail(OperationRules.NotSurgery(request.Recipe));

                // The health tab hides an "Administer drug" entry while the
                // colony holds none of that drug: DrawMedOperationsTab drops
                // every recipe whose PotentiallyMissingIngredients names a
                // drug. A bill added anyway leaves the doctor waiting for
                // stock that may never come, so the write asks the same check
                // (fox ticket #4323).
                if (recipe.Worker is Recipe_AdministerIngestible)
                {
                    string missingDrug = MissingDrug(pawn, recipe);
                    if (missingDrug != null)
                        return ApiResult<PawnOperationsDto>.Fail(
                            OperationRules.NoDrugInColony(recipe.LabelCap, missingDrug)
                        );
                }

                BodyPartRecord part = null;
                if (recipe.targetsBodyPart)
                {
                    List<BodyPartRecord> parts = recipe
                        .Worker.GetPartsToApplyOn(pawn, recipe)
                        .ToList();
                    List<string> labels = parts.Select(p => p.Label).ToList();

                    if (string.IsNullOrWhiteSpace(request.BodyPart))
                        return ApiResult<PawnOperationsDto>.Fail(
                            OperationRules.PartRequired(recipe.LabelCap, labels)
                        );

                    List<BodyPartRecord> matches = parts
                        .Where(p =>
                            string.Equals(p.Label, request.BodyPart.Trim(), StringComparison.OrdinalIgnoreCase)
                            || string.Equals(
                                p.def.defName,
                                request.BodyPart.Trim(),
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .ToList();

                    if (matches.Count == 0)
                        return ApiResult<PawnOperationsDto>.Fail(
                            OperationRules.UnknownPart(request.BodyPart, labels)
                        );
                    if (matches.Count > 1)
                        return ApiResult<PawnOperationsDto>.Fail(
                            OperationRules.AmbiguousPart(
                                request.BodyPart,
                                matches.Select(p => p.Label)
                            )
                        );

                    part = matches[0];
                }

                // The health tab's "Add bill": it adds the bill, sets the part and
                // posts the game's own warnings.
                HealthCardUtility.CreateSurgeryBill(
                    pawn,
                    recipe,
                    part,
                    null,
                    request.SendMessages ?? true
                );

                return ApiResult<PawnOperationsDto>.Ok(ReadOperations(pawn));
            }
            catch (Exception ex)
            {
                return ApiResult<PawnOperationsDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<PawnOperationsDto> Cancel(int pawnId, string operationId)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                    return ApiResult<PawnOperationsDto>.Fail(OperationRules.UnknownPawn(pawnId));
                if (pawn.BillStack == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.NoHealthTracker(pawn.LabelShortCap)
                    );
                if (string.IsNullOrWhiteSpace(operationId))
                    return ApiResult<PawnOperationsDto>.Fail(OperationRules.NoOperationId());

                Bill bill = pawn
                    .BillStack.Bills.FirstOrDefault(b =>
                        string.Equals(
                            b.GetUniqueLoadID(),
                            operationId.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                if (bill == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.UnknownBill(operationId)
                    );

                // The cross next to the bill in the health tab.
                pawn.BillStack.Delete(bill);

                return ApiResult<PawnOperationsDto>.Ok(ReadOperations(pawn));
            }
            catch (Exception ex)
            {
                return ApiResult<PawnOperationsDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<PawnOperationsDto> SetMedicine(MedicinePolicyRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.UnknownCare("")
                    );

                Pawn pawn = PawnHelper.FindPawnById(request.PawnId);
                if (pawn == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.UnknownPawn(request.PawnId)
                    );
                if (pawn.playerSettings == null || pawn.BillStack == null)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.NoHealthTracker(pawn.LabelShortCap)
                    );

                if (
                    !Enum.TryParse(request.Care?.Trim(), true, out MedicalCareCategory care)
                    || !Enum.IsDefined(typeof(MedicalCareCategory), care)
                )
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.UnknownCare(request.Care ?? "")
                    );

                if (pawn.playerSettings.medCare == care)
                    return ApiResult<PawnOperationsDto>.Fail(
                        OperationRules.AlreadyCare(pawn.LabelShortCap, care.ToString())
                    );

                // The health tab's medicine dropdown.
                pawn.playerSettings.medCare = care;

                return ApiResult<PawnOperationsDto>.Ok(ReadOperations(pawn));
            }
            catch (Exception ex)
            {
                return ApiResult<PawnOperationsDto>.Fail(ex.ToString());
            }
        }

        /// <summary>
        /// The drug the recipe is short of, as the game's own ingredient walk
        /// names it, or null when the colony holds some. HealthCardUtility
        /// passes a null billDoer, so a forbidden stack still counts and the
        /// answer matches the list the player sees (fox ticket #4323).
        /// </summary>
        private static string MissingDrug(Pawn pawn, RecipeDef recipe)
        {
            if (recipe.ingredients == null || pawn.MapHeld == null)
                return null;
            return recipe
                .PotentiallyMissingIngredients(null, pawn.MapHeld)
                .FirstOrDefault(def => def != null && def.IsDrug)
                ?.label;
        }

        private static PawnOperationsDto ReadOperations(Pawn pawn)
        {
            return new PawnOperationsDto
            {
                PawnId = pawn.thingIDNumber,
                Name = pawn.LabelShortCap,
                MedicineCare = pawn.playerSettings.medCare.ToString(),
                Operations = pawn
                    .BillStack.Bills.Select(b => new OperationDto
                    {
                        Id = b.GetUniqueLoadID(),
                        Recipe = b.recipe.defName,
                        Label = b.Label,
                        BodyPart = (b as Bill_Medical)?.Part?.Label,
                        BodyPartDef = (b as Bill_Medical)?.Part?.def.defName,
                        Suspended = b.suspended,
                        Completable = b.CompletableEver,
                    })
                    .ToList(),
            };
        }
    }
}
