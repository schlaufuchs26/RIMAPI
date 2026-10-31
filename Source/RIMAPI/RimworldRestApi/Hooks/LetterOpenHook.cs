using System;
using HarmonyLib;
using RIMAPI.Core;
using RIMAPI.Services;
using Verse;

namespace RimworldRestApi.Hooks
{
    // A letter the player opens in game counts as seen, so the next read does
    // not report it as new (ticket #3156). Verse.Letter.OpenLetter is abstract
    // and six types override it in 1.6.4871, so each override is patched by
    // hand rather than through a PatchAll attribute: a name that moves with a
    // game update then costs one warning here instead of failing every patch in
    // the assembly.
    public static class LetterOpenHook
    {
        // Types by name, not by reference: a type this build of the game does
        // not have costs a warning, not a TypeLoadException.
        private static readonly string[] OpeningTypes =
        {
            "Verse.BundleLetter",
            "Verse.ChoiceLetter",
            "Verse.DeathLetter",
            "Verse.NewQuestLetter",
            "Verse.LetterWithTimeout",
            "RimWorld.ChoiceLetter_GrowthMoment",
        };

        public static void Apply(Harmony harmony)
        {
            foreach (var name in OpeningTypes)
            {
                try
                {
                    var type = AccessTools.TypeByName(name);
                    var method = type != null ? AccessTools.Method(type, "OpenLetter") : null;
                    if (method == null)
                    {
                        LogApi.Error(
                            $"[RimworldRestApi] no {name}.OpenLetter: a letter opened in game is not marked seen");
                        continue;
                    }
                    harmony.Patch(method,
                        postfix: new HarmonyMethod(typeof(LetterOpenHook), nameof(Postfix)));
                }
                catch (Exception ex)
                {
                    LogApi.Error($"[RimworldRestApi] could not patch {name}.OpenLetter: {ex}");
                }
            }
        }

        private static void Postfix(Letter __instance)
        {
            if (__instance != null)
                LetterSeen.Mark(__instance.ID);
        }
    }
}
