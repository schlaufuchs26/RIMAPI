using Verse;

namespace RIMAPI.Services
{
    // GasHazard is one gas the walk scan names and the level it costs (ticket
    // #3283). The game keeps four gases in one grid, each with its own effect,
    // so the scan reads all of them at once instead of tox gas alone.
    //
    // The danger is not the gas's damage but what a walk through it costs the
    // pawn, on the same scale the order's max_danger uses: fire is deadly,
    // toxic waste and tox gas some. Blind smoke only costs accuracy and
    // deadlife dust only matters for the corpses it raises, so both are None:
    // the scan names them without ever refusing a walk.
    public sealed class GasHazard
    {
        public GasHazard(string label, GasType gasType, Danger danger, bool needsLungs, bool needsAnomaly)
        {
            Label = label;
            GasType = gasType;
            Danger = danger;
            NeedsLungs = needsLungs;
            NeedsAnomaly = needsAnomaly;
        }

        // Every gas the scan looks at, in the order the hazard text names them.
        public static readonly GasHazard[] All =
        {
            new GasHazard("blind smoke", GasType.BlindSmoke, Danger.None, false, false),
            new GasHazard("tox gas", GasType.ToxGas, Danger.Some, false, false),
            new GasHazard("rot stink", GasType.RotStink, Danger.Some, true, false),
            new GasHazard("deadlife dust", GasType.DeadlifeDust, Danger.None, false, true),
        };

        // What the hazard text calls this gas, e.g. "rot stink (4 cells)".
        public string Label { get; }

        public GasType GasType { get; }

        public Danger Danger { get; }

        // Rot stink only harms a pawn that can still catch lung rot, and
        // deadlife dust only exists with the Anomaly DLC; a gas that cannot
        // touch this pawn is not named.
        public bool NeedsLungs { get; }

        public bool NeedsAnomaly { get; }

        public bool AppliesTo(bool pawnCanCatchLungRot, bool anomalyActive)
        {
            if (NeedsLungs && !pawnCanCatchLungRot) return false;
            if (NeedsAnomaly && !anomalyActive) return false;
            return true;
        }
    }
}
