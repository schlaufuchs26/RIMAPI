using System;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    public partial class ColonistService
    {
        // Slaughter is the one lever a tamed herd has (fox ticket #3507): the
        // hunt designation only reaches wild animals, so a colony animal tamed
        // for wool had no way to become meat. The order is the Animals tab's own
        // Slaughter mark: the designation goes on the animal, and the game's
        // WorkGiver_Slaughter builds the job, so its checks (reach, reservation,
        // the handler's ability) decide. The handler is picked the way the
        // animals work is handed out: the Handling work type first, then the
        // highest Animals skill. When no handler can take it now, the
        // designation stands and the colony's handlers pick it up later.
        public ApiResult MakeJobSlaughter(int mapId, int pawnId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn target = FindPawnOnMap(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");

                string refusal = SlaughterRules.Refusal(new SlaughterRules.AnimalState
                {
                    Animal = target.RaceProps != null && target.RaceProps.Animal,
                    Dead = target.Dead,
                    PlayerFaction = target.Faction != null && target.Faction.IsPlayer,
                    FactionName = target.Faction?.Name,
                    MentalState = target.MentalStateDef?.defName,
                    QuestLodger = target.IsQuestLodger(),
                });
                if (refusal != null)
                {
                    return ApiResult.Fail($"{Describe(target)} cannot be slaughtered: {refusal}");
                }

                if (map.designationManager.DesignationOn(target, DesignationDefOf.Slaughter) == null)
                {
                    map.designationManager.AddDesignation(new Designation(target, DesignationDefOf.Slaughter));
                }

                SlaughterResultDto result = new SlaughterResultDto { PawnId = pawnId, Name = target.LabelShort };
                WorkGiver_Slaughter giver = new WorkGiver_Slaughter();
                Job job = null;
                Pawn handler = null;
                foreach (Pawn candidate in HandlingPawns(map))
                {
                    // WorkGiver_Slaughter.JobOnThing only builds the job; the
                    // acceptance check (capable of violence, reserve, beliefs)
                    // is HasJobOnThing, so asking JobOnThing alone would hand
                    // the order to a colonist who cannot do it (fox #3625).
                    if (giver.HasJobOnThing(candidate, target, true))
                    {
                        handler = candidate;
                        job = giver.JobOnThing(candidate, target, true);
                        break;
                    }
                    result.Refusals.Add(SlaughterRefusal(candidate, target));
                }

                if (job == null)
                {
                    result.Note =
                        "no handler can take it now; the order stands and the colony's handlers will take it "
                        + "when one is free and can reach the animal";
                    return ApiResult.Ok(result);
                }
                if (!handler.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    return ApiResult.Fail($"Failed to assign the slaughter job to {Describe(handler)}");
                }
                result.HandlerId = handler.thingIDNumber;
                result.Handler = handler.LabelShort;
                result.Job = job.def.defName;
                result.Queued = true;
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // SlaughterRefusal names the check that kept a handler off the order,
        // in the order WorkGiver_Slaughter.HasJobOnThing applies them (fox
        // #3625): a colonist incapable of violence never slaughters, and that
        // is not a wait for a free handler.
        private static HandlerRefusalDto SlaughterRefusal(Pawn pawn, Pawn animal)
        {
            int level = pawn.skills?.GetSkill(SkillDefOf.Animals)?.Level ?? 0;
            string reason;
            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                reason = "incapable of violence";
            }
            else if (!pawn.CanReserve(animal, 1, -1, null, true))
            {
                reason = pawn.CanReach(animal, PathEndMode.OnCell, Danger.Deadly)
                    ? "another colonist has him reserved"
                    : "cannot reach him";
            }
            else
            {
                // What remains of HasJobOnThing: the colony's own animal is
                // venerated, so the handler's beliefs keep him off it.
                reason = "his beliefs forbid slaughtering this animal";
            }
            return new HandlerRefusalDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.LabelShort,
                Skill = level,
                SkillLabel = SkillDefOf.Animals.label,
                Reason = reason,
            };
        }
    }
}