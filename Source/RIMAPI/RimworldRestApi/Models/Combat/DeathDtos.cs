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
        public int Tick { get; set; }
        public int VictimId { get; set; }
        public string Victim { get; set; }
        public string VictimKind { get; set; }
        public string VictimFaction { get; set; }
        public int KillerId { get; set; }
        public string Killer { get; set; }
        // Weapon is the def the tale names: what the killer used, empty when
        // the tale has none.
        public string Weapon { get; set; }
        // Def is the tale's defName (KilledLongRange, KilledMelee, ...) and
        // DefLabel its label ("killed at distance").
        public string Def { get; set; }
        public string DefLabel { get; set; }
    }
}
