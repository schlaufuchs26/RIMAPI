using System;
using HarmonyLib;
using RIMAPI.Core;
using RIMAPI.Services;
using Verse;
using Verse.AI;

namespace RimworldRestApi.Hooks
{
    // The job record (ticket #3300) is written where a colonist's job changes,
    // not by sampling: a job that starts and ends between two frames would
    // otherwise never be seen. Both hooks read the pawn afterwards and record
    // only a real change, so the pair firing on one transition costs no line.
    // pawn is not public on the tracker, so the hook reads it through a cached
    // field reference instead of a Traverse lookup per job change.
    internal static class JobTrackerPawn
    {
        internal static readonly AccessTools.FieldRef<Pawn_JobTracker, Pawn> Of =
            AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class JobStartPatch
    {
        static void Postfix(Pawn_JobTracker __instance)
        {
            try
            {
                JobLog.Record(__instance == null ? null : JobTrackerPawn.Of(__instance));
            }
            catch (Exception ex)
            {
                LogApi.Error($"[RimworldRestApi] Error in JobStartPatch.Postfix: {ex}");
            }
        }
    }

    // A pawn who ends his job without a new one is idle, and that is exactly
    // the state a wait report has to name.
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    public static class JobEndPatch
    {
        static void Postfix(Pawn_JobTracker __instance)
        {
            try
            {
                JobLog.Record(__instance == null ? null : JobTrackerPawn.Of(__instance));
            }
            catch (Exception ex)
            {
                LogApi.Error($"[RimworldRestApi] Error in JobEndPatch.Postfix: {ex}");
            }
        }
    }

    // A colonist who dies or leaves the map keeps his last job in the record
    // otherwise, and a wait report counts him as working until the span ends
    // (ticket #3311). The terminal entry ends his record where it happened.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class JobLogDeathPatch
    {
        static void Postfix(Pawn __instance)
        {
            try
            {
                JobLog.RecordDeath(__instance);
            }
            catch (Exception ex)
            {
                LogApi.Error($"[RimworldRestApi] Error in JobLogDeathPatch.Postfix: {ex}");
            }
        }
    }

    // Despawn covers every way off the map that is not death: captured, a
    // caravan, a transport pod.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    public static class JobLogLeavePatch
    {
        static void Postfix(Pawn __instance)
        {
            try
            {
                JobLog.RecordLeave(__instance);
            }
            catch (Exception ex)
            {
                LogApi.Error($"[RimworldRestApi] Error in JobLogLeavePatch.Postfix: {ex}");
            }
        }
    }

    // A load rewinds the game clock and brings back the pawns of another
    // moment, so what the record holds of the timeline before it belongs to
    // nobody: keep only what the loaded world already lived, and the state each
    // pawn is in (ticket #3614). The record itself comes back from the save, so
    // this hook no longer wipes it.
    [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    public static class JobLogResetPatch
    {
        static void Postfix()
        {
            try
            {
                JobLog.AfterLoad();
            }
            catch (Exception ex)
            {
                LogApi.Error($"[RimworldRestApi] Error in JobLogResetPatch.Postfix: {ex}");
            }
        }
    }

    // A new colony has no record of its own: the pawn ids of the colony before
    // it are the ids the new one hands out, so keeping the old lines would
    // credit them with work nobody did (ticket #3614). InitNewGame runs before
    // any pawn exists, so the record starts empty.
    [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
    public static class JobLogNewGamePatch
    {
        static void Postfix()
        {
            try
            {
                JobLog.Forget();
            }
            catch (Exception ex)
            {
                LogApi.Error($"[RimworldRestApi] Error in JobLogNewGamePatch.Postfix: {ex}");
            }
        }
    }
}
