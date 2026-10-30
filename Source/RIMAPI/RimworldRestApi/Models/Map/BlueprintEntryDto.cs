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

        /// <summary>The game's Rot4: 0 north, 1 east, 2 south, 3 west.</summary>
        public int Rotation { get; set; }

        /// <summary>Work still to do in game ticks of work; 0 on a blueprint is nothing to build.</summary>
        public float WorkLeft { get; set; }

        /// <summary>The whole work the build needs, so work_left reads as progress.</summary>
        public float WorkTotal { get; set; }

        /// <summary>The thing's id, the handle the write endpoints use.</summary>
        public int ThingId { get; set; }
    }
}
