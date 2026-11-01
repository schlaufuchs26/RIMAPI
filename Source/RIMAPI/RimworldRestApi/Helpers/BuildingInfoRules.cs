namespace RIMAPI.Helpers
{
    /// <summary>Which answer a building read is built from.</summary>
    public enum BuildingInfoKind
    {
        /// <summary>The plain building DTO: id, def, label, position, bed.</summary>
        Plain,

        /// <summary>A turret's combat, ammo, power and fuel state.</summary>
        Turret,

        /// <summary>A power plant's output and switch state.</summary>
        PowerGenerator,
    }

    /// <summary>
    /// Which DTO a building read answers with (fox ticket #3705). The read built
    /// the turret or generator DTO first and then overwrote it with the plain
    /// one, so a caller never saw IsPowered, PowerConsumption, PowerOutput or
    /// PowerOn: a power read answered as if the building were not a machine at
    /// all, and nothing said so.
    ///
    /// No RimWorld type appears here, so Source/RIMAPI.Tests states the rule
    /// outside the game (see BuildingInfoTests).
    /// </summary>
    public static class BuildingInfoRules
    {
        /// <summary>
        /// The specialised DTO when the building is one the game gives extra
        /// state to, else the plain one. A turret wins over a power plant: no
        /// def in the game is both, and the turret answer is the longer one.
        /// </summary>
        public static BuildingInfoKind KindOf(bool isTurret, bool isPowerPlant)
        {
            if (isTurret) return BuildingInfoKind.Turret;
            if (isPowerPlant) return BuildingInfoKind.PowerGenerator;
            return BuildingInfoKind.Plain;
        }
    }
}
