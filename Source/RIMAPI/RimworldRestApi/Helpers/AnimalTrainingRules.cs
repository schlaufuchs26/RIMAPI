using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The wording of the training card's refusals (fox ticket #4212, gap 5).
    /// The card shows a trick only for an animal that can learn it, and it lists
    /// the masters and areas that exist, so a caller that guessed learns which
    /// names there are.
    ///
    /// Plain C#, no game types, so the refusals are testable without a colony.
    /// </summary>
    public static class AnimalTrainingRules
    {
        public static string UnknownPawn(int id)
        {
            return "no pawn with id " + id + ": the colonists read prints the ids";
        }

        public static string NotAnAnimal(string pawnLabel)
        {
            return pawnLabel + " is not an animal: the training card belongs to animals";
        }

        public static string UnknownTrainable(string name)
        {
            return "no trainable called \"" + name
                + "\": the training read lists the tricks the game has";
        }

        public static string NotShown(string pawnLabel, string trainable)
        {
            return "the training card does not show " + trainable + " for " + pawnLabel
                + ": the animal cannot learn it";
        }

        public static string UnknownMaster(int id)
        {
            return "no pawn with id " + id + ": the colonists read prints the ids";
        }

        public static string UnknownArea(string name, IEnumerable<string> areas)
        {
            return "no area called \"" + name + "\": this map has "
                + string.Join(", ", areas);
        }

        public static string NoMap(string pawnLabel)
        {
            return pawnLabel + " is not on a map, so it has no area to restrict";
        }

        public static string NoMasterNeeded(string pawnLabel)
        {
            return pawnLabel + " does not follow a master: it has not learned obedience";
        }

        public static string NothingToChange()
        {
            return "the call names nothing to change: send follow_drafted or follow_fieldwork";
        }

        public static string AlreadyWanted(string trainable, bool wanted)
        {
            return trainable + " is already " + (wanted ? "wanted" : "not wanted");
        }

        public static string AlreadyArea(string pawnLabel, string area)
        {
            return pawnLabel + " is already restricted to " + area;
        }

        public static string AlreadyMaster(string pawnLabel, string master)
        {
            return pawnLabel + " already follows " + master;
        }
    }
}
