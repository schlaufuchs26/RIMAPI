using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The wording of an operation's refusals (fox ticket #4212, gap 4). The
    /// health tab picks a recipe and a part from lists, so a caller that guessed
    /// a name learns which names the game has.
    ///
    /// Plain C#, no game types, so the refusals are testable without a colony.
    /// </summary>
    public static class OperationRules
    {
        /// <summary>The medicine policies the health tab's dropdown offers.</summary>
        public static readonly string[] CareNames =
        {
            "NoCare",
            "NoMeds",
            "HerbalOrWorse",
            "NormalOrWorse",
            "Best",
        };

        public static string UnknownPawn(int id)
        {
            return "no pawn with id " + id + ": the colonists read prints the ids";
        }

        public static string UnknownRecipe(string name)
        {
            return "no recipe called \"" + name
                + "\": the surgery recipes read lists the operations this pawn can be given";
        }

        public static string NotSurgery(string name)
        {
            return name + " is not an operation, so it cannot be added to a pawn's health tab";
        }

        public static string PartRequired(string recipe, IEnumerable<string> parts)
        {
            return "\"" + recipe + "\" works on one body part; name it with body_part, one of: "
                + string.Join(", ", parts);
        }

        public static string UnknownPart(string part, IEnumerable<string> parts)
        {
            return "no body part called \"" + part + "\" for this operation; it can work on: "
                + string.Join(", ", parts);
        }

        public static string AmbiguousPart(string part, IEnumerable<string> matches)
        {
            return "\"" + part + "\" matches more than one body part ("
                + string.Join(", ", matches) + "); name the label the game shows";
        }

        public static string UnknownBill(string id)
        {
            return "no operation with id \"" + id + "\" on this pawn: the operations read prints the ids";
        }

        public static string NoOperationId()
        {
            return "the call needs the operation's id, as the operations read prints it";
        }

        public static string UnknownCare(string care)
        {
            return "no medicine policy called \"" + care + "\": the health tab offers "
                + string.Join(", ", CareNames);
        }

        public static string NoHealthTracker(string pawnLabel)
        {
            return pawnLabel + " has no health tab: only humanlike pawns take operations and medicine";
        }

        public static string AlreadyCare(string pawnLabel, string care)
        {
            return pawnLabel + " already takes " + care + " medicine";
        }

        /// <summary>
        /// The health tab hides an "Administer drug" entry while the colony
        /// holds none of that drug, so a bill added anyway leaves a doctor
        /// waiting for stock that may never come (fox ticket #4323).
        /// </summary>
        public static string NoDrugInColony(string recipe, string drug)
        {
            return "the colony holds no " + drug + ", so \"" + recipe
                + "\" cannot be given: the health tab lists it once the drug is in stock";
        }
    }
}
