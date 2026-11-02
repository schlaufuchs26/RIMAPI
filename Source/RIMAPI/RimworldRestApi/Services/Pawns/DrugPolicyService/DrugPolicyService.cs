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
    /// The Assign tab's drug policy (fox ticket #4212, gap 3). Until now the
    /// colony could only read that a pawn's mood was sinking; the rules that
    /// decide which drug a pawn takes on its own had no route at all.
    ///
    /// Everything here is the game's own dialog: MakeNewDrugPolicy and CopyFrom
    /// are the New and Duplicate buttons, SetDefault the star, TryDelete the
    /// cross whose "DrugPolicyInUse" sentence names the pawn still following it.
    /// A policy holds one line per drug from the moment it is created, so a write
    /// edits a line and never adds one.
    /// </summary>
    public class DrugPolicyService : IDrugPolicyService
    {
        public ApiResult<List<DrugPolicyDto>> GetAll()
        {
            try
            {
                if (Current.Game == null || Current.Game.drugPolicyDatabase == null)
                    return ApiResult<List<DrugPolicyDto>>.Fail("no colony is loaded");

                DrugPolicy defaultPolicy = Current.Game.drugPolicyDatabase.DefaultDrugPolicy();
                var policies = Current.Game
                    .drugPolicyDatabase.AllPolicies.Select(p => ReadBack(p, p == defaultPolicy))
                    .ToList();
                return ApiResult<List<DrugPolicyDto>>.Ok(policies);
            }
            catch (Exception ex)
            {
                return ApiResult<List<DrugPolicyDto>>.Fail(ex.ToString());
            }
        }

        public ApiResult<DrugPolicyDto> Create(DrugPolicyWriteRequest request)
        {
            try
            {
                if (Current.Game == null || Current.Game.drugPolicyDatabase == null)
                    return ApiResult<DrugPolicyDto>.Fail("no colony is loaded");

                string label = (request?.Label ?? "").Trim();
                if (label.Length == 0)
                    return ApiResult<DrugPolicyDto>.Fail(DrugPolicyRules.NoLabel());

                DrugPolicy policy = Current.Game.drugPolicyDatabase.MakeNewDrugPolicy();

                // The Duplicate button: copy a policy's lines onto the new one.
                if (request.CopyFromPolicyId != 0)
                {
                    DrugPolicy source = FindPolicy(request.CopyFromPolicyId);
                    if (source == null)
                        return ApiResult<DrugPolicyDto>.Fail(
                            DrugPolicyRules.UnknownPolicy(
                                request.CopyFromPolicyId,
                                Current.Game.drugPolicyDatabase.AllPolicies.Select(p => p.id)
                            )
                        );
                    policy.CopyFrom(source);
                }

                policy.label = label;

                string problem = Apply(policy, request.Entries);
                if (problem != null)
                    return ApiResult<DrugPolicyDto>.Fail(problem);

                return ApiResult<DrugPolicyDto>.Ok(ReadBack(policy, false));
            }
            catch (Exception ex)
            {
                return ApiResult<DrugPolicyDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<DrugPolicyDto> Update(DrugPolicyWriteRequest request)
        {
            try
            {
                if (Current.Game == null || Current.Game.drugPolicyDatabase == null)
                    return ApiResult<DrugPolicyDto>.Fail("no colony is loaded");
                if (request == null)
                    return ApiResult<DrugPolicyDto>.Fail(DrugPolicyRules.NoEntries());

                DrugPolicy policy = FindPolicy(request.Id);
                if (policy == null)
                    return ApiResult<DrugPolicyDto>.Fail(
                        DrugPolicyRules.UnknownPolicy(
                            request.Id,
                            Current.Game.drugPolicyDatabase.AllPolicies.Select(p => p.id)
                        )
                    );

                if (!string.IsNullOrWhiteSpace(request.Label))
                    policy.label = request.Label.Trim();

                string problem = Apply(policy, request.Entries);
                if (problem != null)
                    return ApiResult<DrugPolicyDto>.Fail(problem);

                return ApiResult<DrugPolicyDto>.Ok(
                    ReadBack(policy, Current.Game.drugPolicyDatabase.DefaultDrugPolicy() == policy)
                );
            }
            catch (Exception ex)
            {
                return ApiResult<DrugPolicyDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<DrugPolicyDto> Delete(int policyId)
        {
            try
            {
                if (Current.Game == null || Current.Game.drugPolicyDatabase == null)
                    return ApiResult<DrugPolicyDto>.Fail("no colony is loaded");

                DrugPolicy policy = FindPolicy(policyId);
                if (policy == null)
                    return ApiResult<DrugPolicyDto>.Fail(
                        DrugPolicyRules.UnknownPolicy(
                            policyId,
                            Current.Game.drugPolicyDatabase.AllPolicies.Select(p => p.id)
                        )
                    );

                // Read before TryDelete: the database drops the policy, so a read
                // afterwards would answer nothing.
                DrugPolicyDto before = ReadBack(policy, false);

                // The dialog's Delete: a policy a living pawn follows answers the
                // game's own "DrugPolicyInUse" sentence instead of disappearing.
                AcceptanceReport report = Current.Game.drugPolicyDatabase.TryDelete(policy);
                if (!report.Accepted)
                    return ApiResult<DrugPolicyDto>.Fail(report.Reason);

                before.PawnsUsing = 0;
                return ApiResult<DrugPolicyDto>.Ok(before);
            }
            catch (Exception ex)
            {
                return ApiResult<DrugPolicyDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<DrugPolicyDto> SetDefault(int policyId)
        {
            try
            {
                if (Current.Game == null || Current.Game.drugPolicyDatabase == null)
                    return ApiResult<DrugPolicyDto>.Fail("no colony is loaded");

                DrugPolicy policy = FindPolicy(policyId);
                if (policy == null)
                    return ApiResult<DrugPolicyDto>.Fail(
                        DrugPolicyRules.UnknownPolicy(
                            policyId,
                            Current.Game.drugPolicyDatabase.AllPolicies.Select(p => p.id)
                        )
                    );

                if (Current.Game.drugPolicyDatabase.DefaultDrugPolicy() == policy)
                    return ApiResult<DrugPolicyDto>.Fail(
                        DrugPolicyRules.AlreadyDefault(policy.label)
                    );

                // The dialog's star: the default policy is the one a new pawn
                // gets, and the database keeps it in slot 0.
                Current.Game.drugPolicyDatabase.SetDefault(policy);
                return ApiResult<DrugPolicyDto>.Ok(ReadBack(policy, true));
            }
            catch (Exception ex)
            {
                return ApiResult<DrugPolicyDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<DrugPolicyDto> Assign(PawnDrugPolicyRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<DrugPolicyDto>.Fail("the call needs pawn_id and policy_id");

                Pawn pawn = PawnHelper.FindPawnById(request.PawnId);
                if (pawn == null)
                    return ApiResult<DrugPolicyDto>.Fail(DrugPolicyRules.UnknownPawn(request.PawnId));
                if (pawn.drugs == null)
                    return ApiResult<DrugPolicyDto>.Fail(
                        DrugPolicyRules.NoDrugTracker(pawn.LabelShortCap)
                    );

                DrugPolicy policy = FindPolicy(request.PolicyId);
                if (policy == null)
                    return ApiResult<DrugPolicyDto>.Fail(
                        DrugPolicyRules.UnknownPolicy(
                            request.PolicyId,
                            Current.Game.drugPolicyDatabase.AllPolicies.Select(p => p.id)
                        )
                    );

                if (pawn.drugs.CurrentPolicy == policy)
                    return ApiResult<DrugPolicyDto>.Fail(
                        DrugPolicyRules.AlreadyFollowed(pawn.LabelShortCap, policy.label)
                    );

                // The dropdown's write. A policy is not a dose: nothing is taken
                // until the pawn's next drug check, exactly as the player's click
                // behaves.
                pawn.drugs.CurrentPolicy = policy;
                return ApiResult<DrugPolicyDto>.Ok(
                    ReadBack(policy, Current.Game.drugPolicyDatabase.DefaultDrugPolicy() == policy)
                );
            }
            catch (Exception ex)
            {
                return ApiResult<DrugPolicyDto>.Fail(ex.ToString());
            }
        }

        private static DrugPolicy FindPolicy(int id)
        {
            return Current.Game
                .drugPolicyDatabase.AllPolicies.FirstOrDefault(p => p.id == id);
        }

        /// <summary>
        /// Writes one line per drug, in the order the dialog edits them. A field
        /// the caller leaves out keeps the game's value.
        /// </summary>
        private static string Apply(DrugPolicy policy, List<DrugPolicyEntryWriteDto> entries)
        {
            foreach (DrugPolicyEntryWriteDto wanted in entries ?? new List<DrugPolicyEntryWriteDto>())
            {
                if (wanted == null || string.IsNullOrWhiteSpace(wanted.Drug))
                    return DrugPolicyRules.NoEntries();

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(wanted.Drug.Trim());
                if (def == null)
                    return DrugPolicyRules.UnknownDrug(wanted.Drug);
                if (!def.IsDrug)
                    return DrugPolicyRules.NotADrug(wanted.Drug);

                DrugPolicyEntry entry = null;
                for (int i = 0; i < policy.Count; i++)
                {
                    if (policy[i].drug == def)
                    {
                        entry = policy[i];
                        break;
                    }
                }
                if (entry == null)
                    return DrugPolicyRules.NoEntryFor(policy.label, wanted.Drug);

                if (wanted.DaysFrequency.HasValue
                    && !DrugPolicyRules.DaysFrequencyValid(wanted.DaysFrequency.Value))
                    return DrugPolicyRules.DaysFrequencyProblem(wanted.DaysFrequency.Value);
                if (wanted.OnlyIfMoodBelow.HasValue
                    && !DrugPolicyRules.LimitValid(wanted.OnlyIfMoodBelow.Value))
                    return DrugPolicyRules.LimitProblem(
                        "only_if_mood_below",
                        wanted.OnlyIfMoodBelow.Value
                    );
                if (wanted.OnlyIfJoyBelow.HasValue
                    && !DrugPolicyRules.LimitValid(wanted.OnlyIfJoyBelow.Value))
                    return DrugPolicyRules.LimitProblem(
                        "only_if_joy_below",
                        wanted.OnlyIfJoyBelow.Value
                    );

                int maxToCarry = PawnUtility.GetMaxAllowedToPickUp(def);
                if (wanted.TakeToInventory.HasValue
                    && !DrugPolicyRules.TakeToInventoryValid(wanted.TakeToInventory.Value, maxToCarry))
                    return DrugPolicyRules.TakeToInventoryProblem(
                        wanted.TakeToInventory.Value,
                        maxToCarry
                    );

                if (wanted.AllowedForAddiction.HasValue)
                    entry.allowedForAddiction = wanted.AllowedForAddiction.Value;
                if (wanted.AllowedForJoy.HasValue)
                    entry.allowedForJoy = wanted.AllowedForJoy.Value;
                if (wanted.AllowScheduled.HasValue)
                    entry.allowScheduled = wanted.AllowScheduled.Value;
                if (wanted.DaysFrequency.HasValue)
                    entry.daysFrequency = wanted.DaysFrequency.Value;
                if (wanted.OnlyIfMoodBelow.HasValue)
                    entry.onlyIfMoodBelow = wanted.OnlyIfMoodBelow.Value;
                if (wanted.OnlyIfJoyBelow.HasValue)
                    entry.onlyIfJoyBelow = wanted.OnlyIfJoyBelow.Value;
                if (wanted.TakeToInventory.HasValue)
                    entry.takeToInventory = wanted.TakeToInventory.Value;
            }

            return null;
        }

        private static DrugPolicyDto ReadBack(DrugPolicy policy, bool isDefault)
        {
            int usingCount = PawnsFinder
                .AllMapsCaravansAndTravellingTransporters_Alive.Count(p =>
                    p.drugs != null && p.drugs.CurrentPolicy == policy
                );

            var entries = new List<DrugPolicyEntryWriteDto>();
            for (int i = 0; i < policy.Count; i++)
            {
                DrugPolicyEntry e = policy[i];
                if (e.drug == null)
                    continue;
                entries.Add(
                    new DrugPolicyEntryWriteDto
                    {
                        Drug = e.drug.defName,
                        AllowedForAddiction = e.allowedForAddiction,
                        AllowedForJoy = e.allowedForJoy,
                        AllowScheduled = e.allowScheduled,
                        DaysFrequency = e.daysFrequency,
                        OnlyIfMoodBelow = e.onlyIfMoodBelow,
                        OnlyIfJoyBelow = e.onlyIfJoyBelow,
                        TakeToInventory = e.takeToInventory,
                    }
                );
            }

            return new DrugPolicyDto
            {
                Id = policy.id,
                Label = policy.label,
                IsDefault = isDefault,
                PawnsUsing = usingCount,
                Entries = entries,
            };
        }
    }
}
