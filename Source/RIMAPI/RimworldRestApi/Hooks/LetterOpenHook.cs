using System;
using HarmonyLib;
using RIMAPI.Core;
using RIMAPI.Services;
using Verse;

namespace RimworldRestApi.Hooks
{
    // A letter the player opens in game counts as seen, so the next read does
    // not report it as new (ticket #3156). Verse.Letter declares OpenLetter
    // abstract and the overrides sit on several types, so the targets come from
    // LetterOpenTargets rather than a hand-kept list (ticket #3318): patching a
    // type that implements nothing failed on every game start and left the
    // feature silently dead.
    public static class LetterOpenHook
    {
        public static void Apply(Harmony harmony)
        {
            var scan = LetterOpenTargets.ScanLoadedAssemblies(typeof(Letter));
            foreach (var problem in scan.Problems)
            {
                LogApi.Error(
                    "[RimworldRestApi] letter hook: " + problem +
                    "; a letter opened in game is not marked seen");
            }

            var patched = 0;
            foreach (var method in scan.Targets)
            {
                var where = method.DeclaringType != null ? method.DeclaringType.FullName : "?";
                try
                {
                    harmony.Patch(method,
                        postfix: new HarmonyMethod(typeof(LetterOpenHook), nameof(Postfix)));
                    patched++;
                }
                catch (Exception ex)
                {
                    LogApi.Error(
                        "[RimworldRestApi] could not patch " + where + "." +
                        LetterOpenTargets.MethodName + ": " + ex +
                        "; a letter of this type opened in game is not marked seen");
                }
            }

            if (patched == 0)
            {
                LogApi.Error(
                    "[RimworldRestApi] the letter hook patched no " +
                    LetterOpenTargets.MethodName + ": a letter the player opened in " +
                    "game comes back as new to the next letters read");
                return;
            }
            LogApi.Info(
                "[RimworldRestApi] letter hook on " + patched + " " +
                LetterOpenTargets.MethodName + " implementation(s)");
        }

        private static void Postfix(Letter __instance)
        {
            if (__instance != null)
                LetterSeen.Mark(__instance.ID);
        }
    }
}
