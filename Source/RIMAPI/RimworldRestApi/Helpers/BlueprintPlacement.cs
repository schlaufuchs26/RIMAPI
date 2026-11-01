namespace RIMAPI.Helpers
{
    /// <summary>
    /// The game's own build designator does not draw a blueprint for a def with
    /// no construction work: Designator_Build.DesignateSingleCell spawns the
    /// finished building when WorkToBuild is 0, so a sleeping spot stands there
    /// in the same click, with no material and no builder. The blueprint route
    /// drew a plan for it instead, and a colony that needed a bed right now (a
    /// downed raider to capture, a prisoner cell) waited for a builder who may
    /// be asleep or busy elsewhere (fox ticket #3940).
    ///
    /// The rule lives here, away from the game types, so a unit test can pin it
    /// (see BlueprintPlacementTests).
    /// </summary>
    public static class BlueprintPlacement
    {
        /// <summary>
        /// Whether a def goes out finished instead of as a blueprint. The
        /// threshold is the game's own: exactly no construction work.
        /// </summary>
        public static bool PlacesFinished(float workToBuild)
        {
            return workToBuild <= 0f;
        }
    }
}
