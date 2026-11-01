using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    // The ideoligion a colony is founded with (fox tickets #3612, #3927).
    // The fast start skips the game's "choose your ideoligion" page, so the
    // player faction fell back to a randomly generated ideo: whichever
    // precepts the dice gave it, some of them (no meat, slower work, rituals
    // nobody performs) make an unattended colony harder for no reason. This
    // builds a small, harmless one instead, takes a saved ideoligion by name,
    // or takes the meme and the precepts a caller picked.
    public static class IdeoSetup
    {
        // Ideological: no gods, so no deity names or deity moods to satisfy.
        public const string DefaultStructureMeme = "Structure_Ideological";

        // A fluid ideo carries exactly one normal meme. Individualist forces
        // no precept of its own and no ritual.
        public const string DefaultNormalMeme = "Individualist";

        // The default precept set: harmless choices that a run profits from
        // and none that blocks food, work or mood. Everything the game would
        // roll on its own (slavery, skull spikes, work penalties) stays out.
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
            bool named = !string.IsNullOrEmpty(request.IdeoName);
            bool naming = !string.IsNullOrEmpty(request.NewIdeoName);
            if (named && naming)
            {
                throw new Exception(
                    "a saved ideoligion keeps its own name: pass either ideo (start under a saved one) or ideo_name (name the one this call builds), not both");
            }
            if (!ModsConfig.IdeologyActive)
            {
                if (named || naming ||
                    !string.IsNullOrEmpty(request.IdeoMeme) ||
                    (request.IdeoPrecepts != null && request.IdeoPrecepts.Count > 0))
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

            Ideo ideo = named
                ? FindSaved(request.IdeoName)
                : BuildCustom(request.IdeoMeme, request.IdeoPrecepts);

            // A colony that names its own belief (fox ticket #3929): the name
            // is checked before the ideo is added, so a refused name leaves
            // the world exactly as it was.
            if (!named && naming)
            {
                NameNew(ideo, request.NewIdeoName);
            }

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

        // NameNew gives the freshly built ideo the name the call asked for
        // (fox ticket #3929). The checks live in IdeoNameRules so they run in
        // the unit suite; the game's own text for an empty name is translated
        // here, because the helper carries no RimWorld types.
        private static void NameNew(Ideo ideo, string wanted)
        {
            string problem = IdeoNameRules.Check(wanted, ExistingNames(), EmptyNameMessage());
            if (problem != null)
            {
                throw new Exception(problem);
            }
            ideo.name = wanted.Trim();
        }

        // ExistingNames is every name the game keeps, so a duplicate is
        // refused before the colony carries two beliefs with one name.
        private static List<string> ExistingNames()
        {
            var names = new List<string>();
            if (Find.IdeoManager == null)
            {
                return names;
            }
            foreach (Ideo other in Find.IdeoManager.IdeosListForReading)
            {
                if (other != null && !string.IsNullOrEmpty(other.name))
                {
                    names.Add(other.name);
                }
            }
            return names;
        }

        private static string EmptyNameMessage()
        {
            return IdeoNameRules.EmptyKey.CanTranslate()
                ? IdeoNameRules.EmptyKey.Translate().Resolve()
                : null;
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

        // BuildCustom makes the fluid ideo this tool starts a colony with. The
        // meme and the precepts come from the caller when it names them, so
        // the set is a decision instead of whatever the dice rolled; with no
        // names it is the fox default (#3927).
        private static Ideo BuildCustom(string memeName, List<string> preceptNames)
        {
            MemeDef normal = ResolveMeme(memeName);
            List<PreceptDef> precepts = ResolvePrecepts(preceptNames);

            Ideo ideo = IdeoUtility.MakeEmptyIdeo();
            IdeoGenerationParms parms = new IdeoGenerationParms(Faction.OfPlayer.def);

            ideo.foundation.RandomizeCulture(parms);
            ideo.foundation.RandomizePlace();

            ideo.memes.Clear();
            ideo.memes.Add(RequiredMeme(DefaultStructureMeme));
            ideo.memes.Add(normal);
            ideo.SortMemesInDisplayOrder();

            // Fluid before the precepts: the setter creates the development
            // tracker, and a fluid ideo is what can be reformed later.
            ideo.Fluid = true;

            (ideo.foundation as IdeoFoundation_Deity)?.GenerateDeities();
            ideo.foundation.GenerateTextSymbols();
            ideo.foundation.GenerateLeaderTitle();
            ideo.foundation.RandomizeIcon();
            ideo.foundation.RandomizeStyles();

            List<IdeoChoiceRules.PreceptFacts> facts = precepts.Select(Facts).ToList();
            string problem = IdeoChoiceRules.Problem(
                normal.defName,
                ideo.memes.Select(m => m.defName).ToList(),
                facts);
            if (!string.IsNullOrEmpty(problem))
            {
                throw new Exception(problem);
            }

            foreach (PreceptDef def in precepts)
            {
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

        private static IdeoChoiceRules.PreceptFacts Facts(PreceptDef def)
        {
            return new IdeoChoiceRules.PreceptFacts
            {
                Def = def.defName,
                TipLabel = PreceptDescribe.TipLabel(def),
                Issue = def.issue != null ? def.issue.label : "",
                RequiredMemes = PreceptDescribe.RequiredMemes(def),
                ConflictingMemes = PreceptDescribe.ConflictingMemes(def),
            };
        }

        // ResolveMeme reads the one normal meme by defName or by the label the
        // ideoligion window shows. The structure meme is not a choice: a fluid
        // ideo needs one and Ideological is the one this tool builds.
        private static MemeDef ResolveMeme(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return RequiredMeme(DefaultNormalMeme);
            }
            List<MemeDef> normal = DefDatabase<MemeDef>.AllDefsListForReading
                .Where(m => m.category == MemeCategory.Normal && !m.hiddenInChooseMemes)
                .ToList();
            MemeDef found = normal.FirstOrDefault(m =>
                    string.Equals(m.defName, name, StringComparison.OrdinalIgnoreCase))
                ?? normal.FirstOrDefault(m =>
                    string.Equals(m.LabelCap.ToString(), name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                return found;
            }
            MemeDef structure = DefDatabase<MemeDef>.AllDefsListForReading.FirstOrDefault(m =>
                m.category == MemeCategory.Structure &&
                (string.Equals(m.defName, name, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(m.LabelCap.ToString(), name, StringComparison.OrdinalIgnoreCase)));
            if (structure != null)
            {
                throw new Exception(
                    $"\"{name}\" is a structure meme; a fluid ideo keeps the {DefaultStructureMeme} structure, " +
                    "and only the one normal meme is a choice");
            }
            string known = string.Join(", ", normal.Select(m => m.defName).OrderBy(n => n));
            throw new Exception($"no normal meme is named \"{name}\"; the game offers {known}");
        }

        // ResolvePrecepts reads the precept set by defName or by the
        // "Issue: label" the ideoligion window shows. A name that matches no
        // precept is refused with the near misses, because a silently dropped
        // precept would read like an accepted one.
        private static List<PreceptDef> ResolvePrecepts(List<string> names)
        {
            var defs = new List<PreceptDef>();
            List<PreceptDef> all = DefDatabase<PreceptDef>.AllDefsListForReading.ToList();
            List<string> wanted = (names ?? new List<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .ToList();
            if (wanted.Count == 0)
            {
                wanted = DefaultPrecepts.ToList();
            }
            foreach (string name in wanted)
            {
                PreceptDef def = all.FirstOrDefault(p =>
                        string.Equals(p.defName, name, StringComparison.OrdinalIgnoreCase))
                    ?? all.FirstOrDefault(p =>
                        string.Equals(PreceptDescribe.TipLabel(p), name, StringComparison.OrdinalIgnoreCase));
                if (def == null)
                {
                    List<string> near = all
                        .Where(p => PreceptDescribe.TipLabel(p)
                            .IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Select(PreceptDescribe.TipLabel)
                        .Take(8)
                        .ToList();
                    string hint = near.Count > 0
                        ? " Did you mean " + string.Join(", ", near) + "?"
                        : " The precept list is GET /api/v1/defs?include=precept_defs";
                    throw new Exception($"no precept is named \"{name}\".{hint}");
                }
                if (defs.Any(d => d == def))
                {
                    continue;
                }
                defs.Add(def);
            }
            return defs;
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
