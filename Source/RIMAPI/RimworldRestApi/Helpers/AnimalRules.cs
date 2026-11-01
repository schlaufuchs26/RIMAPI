using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;

namespace RIMAPI.Helpers
{
    // The rules behind the animals read (fox ticket #3467): which side an
    // animal is on, the order the lines come in, and the summary line. They stay
    // free of game types so Source/RIMAPI.Tests can state them; the game read
    // lives in PawnInfoService.
    public static class AnimalRules
    {
        public const string Wild = "wild";
        public const string Tame = "tame";

        // Side names whose animal it is. A player-faction animal is tame, an
        // animal of another faction names that faction, and a pawn of no faction
        // is wild; the only one the tame order applies to.
        public static string Side(bool playerFaction, string factionName)
        {
            if (playerFaction) return Tame;
            return string.IsNullOrEmpty(factionName) ? Wild : factionName;
        }

        public static bool IsWild(AnimalDto animal)
        {
            return animal != null && animal.Side == Wild;
        }

        // Listed says whether the animals read lists a pawn. A carried animal is
        // despawned into its carrier's hands, so the spawned-only filter dropped
        // it and a rescue looked like a death for as long as it lasted (fox
        // #3668). Dead and destroyed pawns stay out, and so does one that is on
        // no map and in nobody's hands: a caravan animal is not on this map.
        public static bool Listed(bool spawned, bool carried, bool dead, bool destroyed)
        {
            if (dead || destroyed) return false;
            return spawned || carried;
        }

        // DestinationOf names where a carried pawn is heading, taken from the
        // carrier's own job: the label of the thing its target B names (the bed
        // a rescue walks to), else that target's cell, else "" when the job
        // names neither (fox #3668).
        public static string DestinationOf(string targetLabel, bool cellKnown, int x, int z)
        {
            if (!string.IsNullOrEmpty(targetLabel)) return targetLabel;
            if (cellKnown) return $"{x},{z}";
            return "";
        }

        // Rank puts the animals a run has to decide about first: a manhunter
        // attacks on sight, so it leads; then an animal in somebody's hands,
        // which is the one a run watching a rescue is looking for (fox #3668);
        // then a quest guest, which a run must leave alone; then the wild ones,
        // which are what hunting and taming are about; the colony's own herd
        // last (fox #3611).
        public static int Rank(AnimalDto animal)
        {
            if (animal == null) return 5;
            if (animal.Manhunter) return 0;
            if (!string.IsNullOrEmpty(animal.CarriedBy)) return 1;
            if (animal.QuestLodger) return 2;
            if (animal.Side == Wild) return 3;
            if (animal.Side == Tame) return 5;
            return 4;
        }

        // Sort orders the list in place, by rank and then by label and id so two
        // reads of the same colony come back in the same order.
        public static void Sort(List<AnimalDto> animals)
        {
            if (animals == null) return;
            animals.Sort((a, b) =>
            {
                int byRank = Rank(a).CompareTo(Rank(b));
                if (byRank != 0) return byRank;
                int byLabel = string.CompareOrdinal(LabelOf(a), LabelOf(b));
                if (byLabel != 0) return byLabel;
                return IdOf(a).CompareTo(IdOf(b));
            });
        }

        // Summary is the one line above the list: how many animals stand on the
        // map, how many of them are wild and in a manhunter state, and how many
        // the game already has marked, because a hunt designation from an
        // earlier run keeps the whole herd out of reach of a tame.
        public static string Summary(int mapId, List<AnimalDto> animals)
        {
            int total = animals == null ? 0 : animals.Count;
            if (total == 0)
            {
                return $"No animal stands on map {mapId}: nothing to hunt, tame or pen.";
            }

            int wild = animals.Count(a => a.Side == Wild);
            int tame = animals.Count(a => a.Side == Tame);
            int manhunter = animals.Count(a => a.Manhunter);
            int hunt = animals.Count(a => a.HuntDesignated);
            int tameMarked = animals.Count(a => a.TameDesignated);

            var parts = new List<string>();
            if (wild > 0)
            {
                string clause = $"{wild} wild";
                if (manhunter > 0) clause += $" ({manhunter} manhunter)";
                parts.Add(clause);
            }
            if (tame > 0) parts.Add($"{tame} tame");
            int others = total - wild - tame;
            if (others > 0) parts.Add($"{others} another faction's");

            string line = $"{total} animal{(total == 1 ? "" : "s")} on map {mapId}: " + string.Join(", ", parts);
            var marked = new List<string>();
            if (hunt > 0) marked.Add($"{hunt} marked for hunting");
            if (tameMarked > 0) marked.Add($"{tameMarked} for taming");
            if (marked.Count > 0) line += "; " + string.Join(", ", marked);
            return line;
        }

        private static string LabelOf(AnimalDto animal)
        {
            if (animal == null) return "";
            if (!string.IsNullOrEmpty(animal.Label)) return animal.Label;
            return animal.Def ?? "";
        }

        private static int IdOf(AnimalDto animal)
        {
            return animal == null ? 0 : animal.Id;
        }
    }
}
