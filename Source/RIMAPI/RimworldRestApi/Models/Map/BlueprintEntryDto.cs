namespace RIMAPI.Models.Map
{
    /// <summary>
    /// One blueprint or frame standing on the map (fox ticket #3089). Until this
    /// existed a build order could not be checked: the buildings read lists only
    /// finished buildings, so a table placed on marsh simply vanished from every
    /// answer.
    /// </summary>
    public class BlueprintEntryDto
    {
        /// <summary>
        /// The def the blueprint builds (Wall, Bed, WoodPlankFloor), never the
        /// blueprint's own def ("Blueprint_Wall"): the caller asked for this one.
        /// </summary>
        public string DefName { get; set; }

        /// <summary>The game's label of the def being built.</summary>
        public string Label { get; set; }

        /// <summary>"blueprint" while the material is only planned, "frame" once a colonist started.</summary>
        public string Kind { get; set; }

        /// <summary>The material's defName, null when the def takes none.</summary>
        public string Stuff { get; set; }

        public int X { get; set; }

        public int Z { get; set; }

        /// <summary>
        /// The footprint of what the plan builds, unrotated: a 2x1 bench reads
        /// (2,1) and a 1x2 bed (1,2). The caller swaps it for the east and west
        /// rotations, the same way it reads a building's size. Without it a plan
        /// covered only the cell the game calls its position, so a map art drew
        /// a multi-cell plan as its corner (ticket #3608).
        /// </summary>
        public PositionDto Size { get; set; }

        /// <summary>The game's Rot4: 0 north, 1 east, 2 south, 3 west.</summary>
        public int Rotation { get; set; }

        /// <summary>Work still to do in game ticks of work; 0 on a blueprint is nothing to build.</summary>
        public float WorkLeft { get; set; }

        /// <summary>The whole work the build needs, so work_left reads as progress.</summary>
        public float WorkTotal { get; set; }

        /// <summary>The thing's id, the handle the write endpoints use.</summary>
        public int ThingId { get; set; }

        /// <summary>
        /// How much of its material the build still needs, in units of
        /// <see cref="Stuff"/>; 0 when the def takes no material or the entry is
        /// a frame (its material is delivered into the frame itself).
        /// </summary>
        public int MaterialNeeded { get; set; }

        /// <summary>
        /// How much of that material lies on the map right now, counted the way
        /// the colony's resource counter counts it. A blueprint with fewer units
        /// than <see cref="MaterialNeeded"/> cannot be built until more arrives,
        /// which is what colony 6 sat on for two days (fox ticket #3133).
        /// </summary>
        public int MaterialAvailable { get; set; }
    }
}
