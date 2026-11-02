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

        /// <summary>
        /// The hunted animal's race chance to turn manhunter when it is hurt
        /// (0..1), null for every order that is not a hunt (fox #4183). A run
        /// that marks prey reads here which of the marked animals can turn on
        /// the hunter: a cassowary answers 1.0, an alpaca 0.
        /// </summary>
        public float? ManhunterOnDamageChance { get; set; }
    }

    /// <summary>
    /// One colonist's verdict on the work a designation leaves behind (fox
    /// #3625). The work loop hands a designation out only to a colonist whose
    /// work type is on and who is capable of it, so "the order stands" could
    /// mean "wait" or "nobody will ever". The reason is the game's own check.
    /// </summary>
    public class WorkVerdictDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        /// <summary>The level in the work type's own skill (Plants for plant cutting), 0 when it has none.</summary>
        public int Skill { get; set; }

        /// <summary>The skill's label, e.g. Plants; null when the work type has no skill.</summary>
        public string SkillLabel { get; set; }

        /// <summary>Why the colonist will not take the job; null for a worker.</summary>
        public string Reason { get; set; }
    }
}
