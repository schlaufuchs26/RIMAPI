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
    /// The Assign tab's apparel policy (fox ticket #4212, gap 2). The read
    /// existed and had no write beside it, so a colony could see that its people
    /// wore tattered apparel and could not change the policy that decides it.
    ///
    /// Everything here is the game's own dialog: OutfitDatabase.MakeNewOutfit,
    /// the ThingFilter the Manage outfits dialog edits, and TryDelete, whose
    /// "outfit in use" refusal is the game's sentence.
    /// </summary>
    public class OutfitService : IOutfitService
    {
        public ApiResult<ApparelPolicyResultDto> Create(ApparelPolicyRequest request)
        {
            try
            {
                if (Current.Game == null || Current.Game.outfitDatabase == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail("no colony is loaded");

                string label = (request?.Label ?? "").Trim();
                if (label.Length == 0)
                    return ApiResult<ApparelPolicyResultDto>.Fail(ApparelPolicyRules.NoLabel());

                // MakeNewOutfit is the dialog's New button: it picks the next id
                // and allows every apparel to start from.
                ApparelPolicy policy = Current.Game.outfitDatabase.MakeNewOutfit();
                policy.label = label;

                string problem = Apply(policy, request);
                if (problem != null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(problem);

                return ApiResult<ApparelPolicyResultDto>.Ok(ReadBack(policy));
            }
            catch (Exception ex)
            {
                return ApiResult<ApparelPolicyResultDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<ApparelPolicyResultDto> Update(ApparelPolicyRequest request)
        {
            try
            {
                if (Current.Game == null || Current.Game.outfitDatabase == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail("no colony is loaded");
                if (request == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(ApparelPolicyRules.NothingToChange());

                ApparelPolicy policy = FindPolicy(request.Id);
                if (policy == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(
                        ApparelPolicyRules.UnknownPolicy(
                            request.Id,
                            Current.Game.outfitDatabase.AllOutfits.Select(p => p.id)
                        )
                    );

                if (!string.IsNullOrWhiteSpace(request.Label))
                    policy.label = request.Label.Trim();

                string problem = Apply(policy, request);
                if (problem != null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(problem);

                return ApiResult<ApparelPolicyResultDto>.Ok(ReadBack(policy));
            }
            catch (Exception ex)
            {
                return ApiResult<ApparelPolicyResultDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<ApparelPolicyResultDto> Delete(int policyId)
        {
            try
            {
                if (Current.Game == null || Current.Game.outfitDatabase == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail("no colony is loaded");

                ApparelPolicy policy = FindPolicy(policyId);
                if (policy == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(
                        ApparelPolicyRules.UnknownPolicy(
                            policyId,
                            Current.Game.outfitDatabase.AllOutfits.Select(p => p.id)
                        )
                    );

                // Read the policy before TryDelete: the database clears the
                // filter of a policy it removes, so reading afterwards answers
                // zeros for a policy that was just fine.
                ApparelPolicyResultDto before = ReadBack(policy);

                // The dialog's Delete: a policy a pawn wears answers the game's
                // own "OutfitInUse" report instead of disappearing.
                AcceptanceReport report = Current.Game.outfitDatabase.TryDelete(policy);
                if (!report.Accepted)
                    return ApiResult<ApparelPolicyResultDto>.Fail(report.Reason);

                before.PawnsUsing = 0;
                return ApiResult<ApparelPolicyResultDto>.Ok(before);
            }
            catch (Exception ex)
            {
                return ApiResult<ApparelPolicyResultDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<ApparelPolicyResultDto> Assign(PawnApparelPolicyRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail("the call needs pawn_id and policy_id");

                Pawn pawn = PawnHelper.FindPawnById(request.PawnId);
                if (pawn == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(ApparelPolicyRules.UnknownPawn(request.PawnId));
                if (pawn.outfits == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(
                        ApparelPolicyRules.NoOutfitTracker(pawn.LabelShortCap)
                    );

                ApparelPolicy policy = FindPolicy(request.PolicyId);
                if (policy == null)
                    return ApiResult<ApparelPolicyResultDto>.Fail(
                        ApparelPolicyRules.UnknownPolicy(
                            request.PolicyId,
                            Current.Game.outfitDatabase.AllOutfits.Select(p => p.id)
                        )
                    );

                if (pawn.outfits.CurrentApparelPolicy == policy)
                    return ApiResult<ApparelPolicyResultDto>.Fail(
                        ApparelPolicyRules.AlreadyWorn(pawn.LabelShortCap, policy.label)
                    );

                // The dropdown's write. A policy is not an order: nothing in the
                // game happens until the next apparel check, which is the same
                // behaviour as the player's click.
                pawn.outfits.CurrentApparelPolicy = policy;
                return ApiResult<ApparelPolicyResultDto>.Ok(ReadBack(policy));
            }
            catch (Exception ex)
            {
                return ApiResult<ApparelPolicyResultDto>.Fail(ex.ToString());
            }
        }

        private static ApparelPolicy FindPolicy(int id)
        {
            return Current.Game.outfitDatabase.AllOutfits.FirstOrDefault(p => p.id == id);
        }

        /// <summary>
        /// The filter edits, in the order the dialog applies them: the bulk
        /// switches first, then the named defs, then the two ranges.
        /// </summary>
        private static string Apply(ApparelPolicy policy, ApparelPolicyRequest request)
        {
            if (request == null)
                return null;

            ThingFilter filter = policy.filter;

            if (request.DisallowAll)
                filter.SetDisallowAll();
            else if (request.AllowAllApparel)
                filter.SetAllow(ThingCategoryDefOf.Apparel, true);

            foreach (string name in request.AllowThingDefNames ?? new List<string>())
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                    return ApparelPolicyRules.UnknownThingDef(name);
                if (def.apparel == null)
                    return ApparelPolicyRules.NotApparel(name);
                filter.SetAllow(def, true);
            }

            foreach (string name in request.DisallowThingDefNames ?? new List<string>())
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                    return ApparelPolicyRules.UnknownThingDef(name);
                if (def.apparel == null)
                    return ApparelPolicyRules.NotApparel(name);
                filter.SetAllow(def, false);
            }

            foreach (string name in request.AllowSpecialFilterDefNames ?? new List<string>())
            {
                SpecialThingFilterDef def = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(name);
                if (def == null)
                    return ApparelPolicyRules.UnknownSpecialFilter(name);
                filter.SetAllow(def, true);
            }

            foreach (string name in request.DisallowSpecialFilterDefNames ?? new List<string>())
            {
                SpecialThingFilterDef def = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(name);
                if (def == null)
                    return ApparelPolicyRules.UnknownSpecialFilter(name);
                filter.SetAllow(def, false);
            }

            if (request.HitPointsMin.HasValue || request.HitPointsMax.HasValue)
            {
                Verse.FloatRange current = filter.AllowedHitPointsPercents;
                float min = request.HitPointsMin ?? current.min;
                float max = request.HitPointsMax ?? current.max;
                if (!ApparelPolicyRules.HitPointsValid(min, max))
                    return ApparelPolicyRules.HitPointsProblem(min, max);
                filter.AllowedHitPointsPercents = new Verse.FloatRange(min, max);
            }

            if (!string.IsNullOrWhiteSpace(request.QualityMin) || !string.IsNullOrWhiteSpace(request.QualityMax))
            {
                QualityRange current = filter.AllowedQualityLevels;
                QualityCategory min = current.min;
                QualityCategory max = current.max;
                if (!string.IsNullOrWhiteSpace(request.QualityMin)
                    && !Enum.TryParse(request.QualityMin.Trim(), true, out min))
                    return ApparelPolicyRules.QualityProblem(request.QualityMin);
                if (!string.IsNullOrWhiteSpace(request.QualityMax)
                    && !Enum.TryParse(request.QualityMax.Trim(), true, out max))
                    return ApparelPolicyRules.QualityProblem(request.QualityMax);
                if (min > max)
                    return ApparelPolicyRules.QualityProblem(request.QualityMin + " above " + request.QualityMax);
                filter.AllowedQualityLevels = new QualityRange(min, max);
            }

            return null;
        }

        /// <summary>The policy as the game holds it now, plus how many wear it.</summary>
        private static ApparelPolicyResultDto ReadBack(ApparelPolicy policy)
        {
            int usingCount = PawnsFinder
                .AllMapsCaravansAndTravellingTransporters_Alive.Count(p =>
                    p.outfits != null && p.outfits.CurrentApparelPolicy == policy
                );

            return new ApparelPolicyResultDto
            {
                Id = policy.id,
                Label = policy.label,
                AllowedDefCount = policy.filter.AllowedDefCount,
                AllowedHitPointsMin = policy.filter.AllowedHitPointsPercents.min,
                AllowedHitPointsMax = policy.filter.AllowedHitPointsPercents.max,
                AllowedQualityMin = policy.filter.AllowedQualityLevels.min.ToString(),
                AllowedQualityMax = policy.filter.AllowedQualityLevels.max.ToString(),
                AllowedThingDefNames = policy
                    .filter.AllowedThingDefs.Select(d => d.defName)
                    .ToList(),
                DisallowedSpecialFilterDefNames = DefDatabase<SpecialThingFilterDef>
                    .AllDefs.Where(sf => !policy.filter.Allows(sf))
                    .Select(sf => sf.defName)
                    .ToList(),
                PawnsUsing = usingCount,
            };
        }
    }
}
