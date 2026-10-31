using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// One rule for "this pawn is an enemy of the colony", shared by the
    /// combat read and the cheap headcount (fox ticket #3451). The headcount
    /// counted humanlikes only, so a manhunter warg raid reached the tool as
    /// "0 hostiles": the digest never named it and a play run only found out
    /// when a colonist was already bleeding.
    ///
    /// The rule is the combat read's: an enemy faction, or an animal the game
    /// has turned manhunter. Mechanoids and hostile insects are covered by the
    /// faction test, which the humanlike filter also used to drop.
    /// </summary>
    public static class Hostility
    {
        public static bool IsHostileToColony(Pawn pawn)
        {
            if (pawn == null) return false;
            if (IsManhunter(pawn)) return true;
            return pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
        }

        public static bool IsManhunter(Pawn pawn)
        {
            if (pawn == null || pawn.RaceProps == null || !pawn.RaceProps.Animal) return false;
            MentalStateDef state = pawn.MentalStateDef;
            return state == MentalStateDefOf.Manhunter || state == MentalStateDefOf.ManhunterPermanent;
        }
    }
}
