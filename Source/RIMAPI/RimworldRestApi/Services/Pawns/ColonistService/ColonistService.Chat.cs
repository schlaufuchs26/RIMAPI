using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    // A stranger who wants to be talked into joining (ticket #3275): the
    // Anomaly creep joiner behind the "Lone genius" letter stands at the base
    // door and the game recruits him through its own social interaction, the
    // right-click "Chat with" entry on a selected colonist. No order reached
    // him: `tame` refuses a humanlike who is not wild, `arrest` needs a hostile
    // and the letter offers only Close and Jump. After one in-game day the alert
    // "Visitor ignored" ends him, so the recruit was lost. This queues exactly
    // the job that float menu builds (JobDefOf.TalkCreepJoiner, playerForced),
    // with the same two checks: the colonist can reach him and is able to talk.
    public partial class ColonistService
    {
        public ApiResult MakeJobChat(int mapId, int pawnId, int chatterId)
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult.Fail(mapError);
                Pawn target = PawnHelper.FindPawnForOrder(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                // A carried visitor is out of reach of any chatter, and the old
                // lookup answered "not found" for him (fox #3719).
                string carried = CombatRefusal.CarriedRefusal(
                    Describe(target), target.CarriedBy?.LabelShort, "chat with");
                if (carried != null) return ApiResult.Fail(carried);

                Pawn_CreepJoinerTracker joiner = target.creepjoiner;
                if (joiner == null)
                {
                    return ApiResult.Fail(
                        $"{Describe(target)} is not a stranger who wants to talk: only a visitor who arrived "
                        + "to be talked into joining waits for a chat (`people` marks him)");
                }
                if (joiner.Disabled)
                {
                    return ApiResult.Fail(
                        $"{Describe(target)} is not waiting to talk any more: he left, joined or is no longer on the map");
                }
                if (!joiner.IsOnEntryLord)
                {
                    return ApiResult.Fail(
                        $"{Describe(target)} was spoken to already: the offer letter decides it "
                        + "(`letters` lists it, `letter_answer` presses accept or reject)");
                }

                Pawn chatter = chatterId > 0 ? PawnHelper.FindPawnForOrder(map, chatterId) : PickChatter(map, target);
                ChatResultDto result = new ChatResultDto { PawnId = pawnId, Name = target.LabelShort };
                if (chatter == null)
                {
                    result.Note =
                        chatterId > 0
                            ? $"no colonist with ID={chatterId} is on the map"
                            : "no colonist can reach him and talk right now";
                    return ApiResult.Ok(result);
                }
                if (chatter == target)
                {
                    return ApiResult.Fail($"Pawn {pawnId} cannot chat with itself");
                }
                string refusal = ChatRefusal(chatter, target);
                if (refusal != null) return ApiResult.Fail(refusal);

                Job job = JobMaker.MakeJob(JobDefOf.TalkCreepJoiner, target);
                job.playerForced = true;
                if (!chatter.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    return ApiResult.Fail($"Failed to assign the talk job to {Describe(chatter)}");
                }

                result.ChatterId = chatter.thingIDNumber;
                result.Chatter = chatter.LabelShort;
                result.Job = job.def.defName;
                result.Queued = true;
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // ChatRefusal is the game's own two conditions on the float menu entry,
        // in its own terms: a colonist who cannot talk (downed, a baby) or who
        // cannot reach the visitor gets no chat, and the answer says which.
        private static string ChatRefusal(Pawn chatter, Pawn target)
        {
            if (chatter.Downed) return $"{Describe(chatter)} is downed, so he cannot walk over and talk";
            if (chatter.InMentalState) return $"{Describe(chatter)} is in a mental state, so he cannot talk";
            if (!chatter.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                return $"{Describe(chatter)} is not able to talk";
            if (!chatter.CanReach(target, PathEndMode.OnCell, Danger.Deadly))
                return $"{Describe(chatter)} cannot reach {Describe(target)}";
            return null;
        }

        // PickChatter takes the nearest free colonist who can reach the visitor
        // and talk, the same colonist the player would select for the float
        // menu. An ordered job runs even with every work type off, so the work
        // settings do not matter here.
        private static Pawn PickChatter(Map map, Pawn target)
        {
            return map.mapPawns.FreeColonistsSpawned
                .Where(p => p != target && !p.Downed && !p.InMentalState)
                .Where(p => p.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                .Where(p => p.CanReach(target, PathEndMode.OnCell, Danger.Deadly))
                .OrderBy(p => (p.Position - target.Position).LengthHorizontalSquared)
                .FirstOrDefault();
        }
    }
}
