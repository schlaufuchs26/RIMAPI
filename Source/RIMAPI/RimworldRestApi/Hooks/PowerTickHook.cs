using HarmonyLib;
using RIMAPI.Helpers;
using RimWorld;
using Verse;

namespace RimworldRestApi.Hooks
{
    // Records when a map's power grid ticked (fox ticket #3781). PowerNetTick
    // is where the net recomputes the figures the power read reports, so its
    // game tick is the age of every rate that read prints. A colony loaded and
    // paused never reaches it, which is exactly the state a fresh debug game
    // and a just-loaded save are in.
    [HarmonyPatch(typeof(PowerNet), nameof(PowerNet.PowerNetTick))]
    public static class PowerNetTickPatch
    {
        static void Postfix(PowerNet __instance)
        {
            Map map = __instance?.Map;
            TickManager ticks = Find.TickManager;
            if (map == null || ticks == null)
                return;

            PowerTick.Record(map.uniqueID, ticks.TicksGame);
        }
    }
}
