namespace RIMAPI.Models
{
    /// <summary>
    /// One designation on the map: an order the player (or an API caller) left
    /// for the colony to carry out. A designation targets either a cell (mine,
    /// a floor to remove) or a thing (a tree to cut, a building to take apart,
    /// an animal to hunt), so a caller can see what is already marked instead
    /// of guessing from the work that follows (fox ticket #3060).
    /// </summary>
    public class DesignationDto
    {
        /// <summary>The designation's defName, e.g. CutPlant, Mine, HarvestPlant, Hunt, Deconstruct.</summary>
        public string DefName { get; set; }

        /// <summary>The def's human label as the game shows it.</summary>
        public string Label { get; set; }

        /// <summary>"cell" for a cell designation, "thing" for one that hangs on a thing.</summary>
        public string Target { get; set; }

        public int X { get; set; }

        public int Z { get; set; }

        /// <summary>The targeted thing's id, 0 for a cell designation.</summary>
        public int ThingId { get; set; }

        /// <summary>The targeted thing's defName, null for a cell designation.</summary>
        public string ThingDefName { get; set; }

        /// <summary>The targeted thing's short label, null for a cell designation.</summary>
        public string ThingLabel { get; set; }
    }
}
