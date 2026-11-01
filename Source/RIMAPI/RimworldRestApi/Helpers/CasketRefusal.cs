using System;

namespace RIMAPI.Helpers
{
    // Why a casket refused a thing (fox ticket #3784). Building_Grave.Accepts
    // asks four questions in order: does the container take the thing at all,
    // is the grave empty, does an assigned grave's colonist match the corpse,
    // and only for an unassigned grave does the storage filter decide. One
    // sentence for all four ("the storage filter, the grave's assignment or
    // the item's own settings say no") left a run with three empty graves and
    // no way to tell which lever to pull.
    //
    // The facts arrive as plain values, so the wording is testable without a
    // game; ColonistService fills them from the live objects.
    public static class CasketRefusal
    {
        public class Casket
        {
            // Label is the game's own name for the building ("Grave"), Id the
            // thing id a call addresses.
            public string Label = "";
            public int Id;

            // Holds is the corpse inside as the read prints it, empty when
            // the grave is free. AssignedPawn is the colonist the grave
            // belongs to, empty when it takes any corpse.
            public string Holds = "";
            public string AssignedPawn = "";

            // Filter is the grave's storage filter as the game summarises it.
            // It decides nothing while the grave is assigned.
            public string Filter = "";
        }

        // Cause is the refusal for a casket that does not accept a thing, or
        // null when the casket's own facts explain nothing and the caller
        // keeps its general sentence.
        public static string Cause(
            Casket casket,
            string thingLabel,
            bool thingIsCorpse,
            string corpsePawn
        )
        {
            string name = string.IsNullOrEmpty(casket.Label) ? "The grave" : casket.Label;
            string where = casket.Id > 0 ? name + " #" + casket.Id : name;

            if (!string.IsNullOrEmpty(casket.Holds))
            {
                return where
                    + " already holds "
                    + casket.Holds
                    + ", and a grave takes one body; `build name=Grave` places another grave, and "
                    + "`job item_id=<corpse>` without dest_id takes a free one";
            }

            if (!string.IsNullOrEmpty(casket.AssignedPawn))
            {
                if (!thingIsCorpse)
                {
                    return where
                        + " is assigned to "
                        + casket.AssignedPawn
                        + ", and an assigned grave takes only that colonist's corpse, not "
                        + thingLabel
                        + "; `job item_id=<corpse>` without dest_id takes a free grave, and "
                        + "`build name=Grave` places one";
                }
                return where
                    + " is assigned to "
                    + casket.AssignedPawn
                    + ", and this corpse is "
                    + corpsePawn
                    + "; an assigned grave takes only the colonist it is assigned to, so "
                    + "`job item_id=<corpse>` without dest_id takes a free grave, and "
                    + "`build name=Grave` places one";
            }

            if (!string.IsNullOrEmpty(casket.Filter))
            {
                return where
                    + "'s storage filter ("
                    + casket.Filter
                    + ") does not allow "
                    + thingLabel
                    + "; the filter is the grave's own storage tab, and `job item_id=<corpse>` "
                    + "without dest_id takes a grave whose filter allows it";
            }

            return null;
        }
    }
}
