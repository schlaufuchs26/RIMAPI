using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    // The ideoligion a colony is founded with (fox ticket #3612). The fast
    // start skips the game's "choose your ideoligion" page, so the player
    // faction fell back to a randomly generated ideo: whichever precepts the
    // dice gave it, some of them (no meat, slower work, rituals nobody
    // performs) make an unattended colony harder for no reason. This builds a
    // small, harmless one instead, or takes a saved ideoligion by name.
    public static class IdeoSetup
    {
        // Ideological: no gods, so no deity names or deity moods to satisfy.
        public const string DefaultStructureMeme = "Structure_Ideological";

        // A fluid ideo carries exactly one normal meme. Individualist forces
        // no precept of its own and no ritual.
        public const string DefaultNormalMeme = "Individualist";

        // The default precept set: four harmless choices that a run profits
        // from and none that blocks food, work or mood. Everything the game
        // would roll on its own (slavery, skull spikes, work penalties) stays
        // out.
        public static readonly string[] DefaultPrecepts =
        {
            "Research_VeryFast",
            "Slavery_Abhorrent",
            "Skullspike_Disapproved",
            "Nudity_Male_NoRules",
            "Nudity_Female_NoRules",
        };

        // Apply replaces the ideo the faction got during world generation with
        // the requested one. Called after GenerateWorld (the player faction
        // exists then) and before Scenario.PostIdeoChosen, which hands the
        // starting pawns their ideo.
        public static void Apply(NewGameStartRequestDto request)
        {
            if (!ModsConfig.IdeologyActive)
            {
                if (!string.IsNullOrEmpty(request.IdeoName))
                {
                    throw new Exception(
                        "an ideoligion was named, but the Ideology expansion is not active");
                }
                return;
            }

            Faction player = Faction.OfPlayerSilentFail;
            if (player == null || player.ideos == null)
            {
                throw new Exception("no player faction exists yet, so no ideoligion can be chosen");
            }
            if (Find.IdeoManager == null)
            {
                throw new Exception("the world has no ideoligion manager yet");
            }

            Ideo ideo = string.IsNullOrEmpty(request.IdeoName)
                ? BuildDefault()
                : FindSaved(request.IdeoName);

            // The random ideo world generation made for the player is dropped
            // rather than kept as a minor ideo: the colony starts with exactly
            // one belief, the one this call names.
            player.ideos.RemoveAll();
            Find.IdeoManager.Add(ideo);
            foreach (Ideo other in Find.IdeoManager.IdeosListForReading)
            {
                other.initialPlayerIdeo = false;
            }
            ideo.initialPlayerIdeo = true;
            player.ideos.SetPrimary(ideo);
            Find.IdeoManager.RemoveUnusedStartingIdeos();
        }

        // FindSaved resolves a name against the ideoligions the game keeps
        // (the list the "load ideoligion" dialog shows).
        private static Ideo FindSaved(string name)
        {
            List<Ideo> all = Find.IdeoManager.IdeosListForReading
                .Where(i => !i.hidden)
                .ToList();
            Ideo found = all.FirstOrDefault(i =>
                string.Equals(i.name, name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                return found;
            }
            string known = all.Count > 0
                ? string.Join(", ", all.Select(i => "\"" + i.name + "\""))
                : "none";
            throw new Exception(
                $"no saved ideoligion is named \"{name}\"; the game knows {known}. " +
                "Leave ideo empty for the fox default");
        }

        // BuildDefault makes the fluid ideo this tool starts a colony with.
        // The precepts are added by hand instead of rolled, so the set is the
        // same every time and stays free of the expensive ones.
        private static Ideo BuildDefault()
        {
            Ideo ideo = IdeoUtility.MakeEmptyIdeo();
            IdeoGenerationParms parms = new IdeoGenerationParms(Faction.OfPlayer.def);

            ideo.foundation.RandomizeCulture(parms);
            ideo.foundation.RandomizePlace();

            ideo.memes.Clear();
            ideo.memes.Add(RequiredMeme(DefaultStructureMeme));
            ideo.memes.Add(RequiredMeme(DefaultNormalMeme));
            ideo.SortMemesInDisplayOrder();

            // Fluid before the precepts: the setter creates the development
            // tracker, and a fluid ideo is what can be reformed later.
            ideo.Fluid = true;

            (ideo.foundation as IdeoFoundation_Deity)?.GenerateDeities();
            ideo.foundation.GenerateTextSymbols();
            ideo.foundation.GenerateLeaderTitle();
            ideo.foundation.RandomizeIcon();
            ideo.foundation.RandomizeStyles();

            foreach (string defName in DefaultPrecepts)
            {
                PreceptDef def = DefDatabase<PreceptDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    Log.Warning($"[RIMAPI] default ideoligion precept {defName} is not in this game; skipped");
                    continue;
                }
                ideo.AddPrecept(PreceptMaker.MakePrecept(def), init: true, Faction.OfPlayer.def);
            }

            ideo.RecachePrecepts();
            ideo.RegenerateDescription(force: true);

            // A fresh fluid ideo starts at 0 of the 10 development points a
            // reform costs, so it could not be reformed at all. Grant the
            // first reform and leave the points unspent: the reform itself is
            // what spends them (ticket #3612).
            ideo.development.TryAddDevelopmentPoints(
                ideo.development.NextReformationDevelopmentPoints);
            return ideo;
        }

        private static MemeDef RequiredMeme(string defName)
        {
            MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(defName);
            if (meme == null)
            {
                throw new Exception($"the game has no meme {defName}, so no default ideoligion can be built");
            }
            return meme;
        }
    }
}
