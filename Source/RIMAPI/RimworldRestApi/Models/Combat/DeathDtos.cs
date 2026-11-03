using RIMAPI.Core;

namespace RIMAPI.Models
{
    // One pawn the game recorded as killed by another (fox ticket #3643). The
    // fight block of a wait said "gone" for a raider who died, which reads the
    // same as one who fled; the game's own kill tales carry who killed whom and
    // with what, and this read answers them.
    //
    // The tales are the game's memory of a kill: KilledLongRange, KilledMelee,
    // KilledMortar, KilledMajorThreat, KilledCapacity. A death nobody caused
    // (blood loss, an infection) has no tale; the archive's death letter
    // answers that one, so a caller reads both.
    public class DeathDto
    {
        // Tick is the game's own tick (TicksGame), the same clock the game
        // state reads, so a caller can ask for what happened since its last
        // look.
        [ParameterDescription("The game tick the pawn died on.")]
        public int Tick { get; set; }
        [ParameterDescription("The numeric id of the pawn that died.")]
        public int VictimId { get; set; }
        [ParameterDescription("The name of the pawn that died.")]
        public string Victim { get; set; }
        [ParameterDescription("What the victim was, as the game names it.")]
        public string VictimKind { get; set; }
        [ParameterDescription("The victim's faction, as the game names it.")]
        public string VictimFaction { get; set; }
        [ParameterDescription("The numeric id of the pawn that killed it.")]
        public int KillerId { get; set; }
        [ParameterDescription("The name of the pawn that killed it.")]
        public string Killer { get; set; }
        // Weapon is the def the tale names: what the killer used, empty when
        // the tale has none.
        [ParameterDescription("The weapon that killed it, as the game names it.")]
        public string Weapon { get; set; }
        // Def is the tale's defName (KilledLongRange, KilledMelee, ...) and
        // DefLabel its label ("killed at distance").
        [ParameterDescription("The damage def that killed it, e.g. Bullet.")]
        public string Def { get; set; }
        [ParameterDescription("The damage def's label, as the game shows it.")]
        public string DefLabel { get; set; }
    }
}
