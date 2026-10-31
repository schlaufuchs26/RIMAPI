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

    // A load rewinds the game clock and brings back the pawns of another
    // moment, so the record of the colony that was there before belongs to
    // nobody: start it over when a game finishes loading.
    [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    public static class JobLogResetPatch
    {
        static void Postfix()
        {
            try
            {
                JobLog.Forget();
            }
            catch (Exception ex)
            {
                LogApi.Error($"[RimworldRestApi] Error in JobLogResetPatch.Postfix: {ex}");
            }
        }
    }
}
