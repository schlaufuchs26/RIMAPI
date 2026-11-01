using System;
using HarmonyLib;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RimWorld;
using Verse;

namespace RimworldRestApi.Hooks
{
    /// <summary>
    /// The finish-frame driver rolls a construction failure with
    /// 1 - ConstructSuccessChance^(work / WorkToBuild); a frame whose building
    /// needs no work (WorkToBuild 0, e.g. AnimalSleepingSpot) divides by zero,
    /// so the roll succeeds on every tick and the frame is destroyed and its
    /// blueprint placed again without end (fox ticket #3610). Finishing such a
    /// frame is the only outcome the game can have, since no work is left.
    /// </summary>
    [HarmonyPatch(typeof(Frame), nameof(Frame.FailConstruction))]
    public static class ZeroWorkFrameFinishPatch
    {
        static bool Prefix(Frame __instance, Pawn worker)
        {
            try
            {
                if (__instance == null || __instance.Destroyed)
                    return true;
                if (!FrameConstruction.FinishInsteadOfFailing(__instance.WorkToBuild, worker == null))
                    return true;
                // Log.Message, not LogApi.Info: the chatty levels are gated by
                // LogApi.IsLogging and this line has to land, because it is the
                // only trace of a frame that the game would otherwise destroy
                // again and again.
                Verse.Log.Message("[RIMAPI | INFO] " + FrameConstruction.LogLine(
                    __instance.BuildDef == null ? "?" : __instance.BuildDef.defName,
                    __instance.WorkToBuild));
                __instance.CompleteConstruction(worker);
                return false;
            }
            catch (Exception ex)
            {
                // Never fall back into the loop: the frame is gone or cannot be
                // completed, and failing it again would only place the
                // blueprint for another try.
                LogApi.Error($"[RimworldRestApi] zero-work frame: finishing it failed: {ex}");
                return false;
            }
        }
    }
}
