using System.Collections.Generic;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The incapability list of a pawn, in the words the game's own character
    /// card uses (fox, #3360). A pawn whose backstory disables violence
    /// carries no trait that says so, so his trait list reads like any other
    /// one's and a caller drafts him for a raid that never fires.
    ///
    /// The file touches no pawn, only the game's WorkTags mask, so the unit
    /// tests cover it against the stubs in Source/RIMAPI.Tests.
    /// </summary>
    public static class IncapabilityTags
    {
        // The game's tags in the order its incapability list shows them, each
        // with the label that list uses.
        private static readonly (WorkTags Tag, string Label)[] Labels =
        {
            (WorkTags.Violent, "violence"),
            (WorkTags.ManualSkilled, "skilled labor"),
            (WorkTags.ManualDumb, "dumb labor"),
            (WorkTags.Caring, "caring"),
            (WorkTags.Social, "social"),
            (WorkTags.Intellectual, "intellectual"),
            (WorkTags.Artistic, "artistic"),
            (WorkTags.Crafting, "crafting"),
            (WorkTags.Animals, "animals"),
            (WorkTags.Cooking, "cooking"),
            (WorkTags.Firefighting, "firefighting"),
            (WorkTags.Cleaning, "cleaning"),
            (WorkTags.Hauling, "hauling"),
            (WorkTags.PlantWork, "plant work"),
            (WorkTags.Mining, "mining"),
            (WorkTags.Hunting, "hunting"),
            (WorkTags.Shooting, "shooting"),
            (WorkTags.Constructing, "construction"),
            (WorkTags.Commoner, "commoner"),
        };

        /// <summary>
        /// The labels of the tags a pawn's disable mask holds, in the game's
        /// order. An empty mask is an empty list, never a null.
        /// </summary>
        public static List<string> From(WorkTags disabled)
        {
            List<string> labels = new List<string>();
            foreach (var entry in Labels)
            {
                if ((disabled & entry.Tag) != 0) labels.Add(entry.Label);
            }
            return labels;
        }

        /// <summary>
        /// False when the game disables violence for the pawn: no draft and no
        /// order can make him shoot, whatever he carries.
        /// </summary>
        public static bool CanFight(WorkTags disabled)
        {
            return (disabled & WorkTags.Violent) == 0;
        }
    }
}
