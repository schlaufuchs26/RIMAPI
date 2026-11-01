namespace RIMAPI.Helpers
{
    // What makes a pawn a threat to the colony (fox ticket #3630). The rule used
    // to be "an enemy faction, or an animal the game turned manhunter", which
    // missed the pawn that was actually hurting the colony: a wild man has no
    // faction, so colony 14's DiLeo (id 54348) beat Veniamin from 100% to 40%
    // health while the fight read said "0 hostiles" and the wait ran its full
    // hour. A pawn's own job says what it is doing, so an attack on the colony
    // counts as hostility.
    //
    // The checks stay free of game types so Source/RIMAPI.Tests can state them;
    // Hostility maps a Pawn onto PawnState.
    public static class HostilityRules
    {
        /// <summary>One pawn, as the game reports it.</summary>
        public struct PawnState
        {
            /// <summary>The pawn belongs to the colony: the colonists list owns it.</summary>
            public bool Colonist;
            /// <summary>An animal, the only kind the manhunter states apply to.</summary>
            public bool Animal;
            /// <summary>A humanlike of no faction who lives wild on the map.</summary>
            public bool WildMan;
            /// <summary>The colony holds the pawn prisoner.</summary>
            public bool Prisoner;
            /// <summary>The colony holds the pawn as a slave.</summary>
            public bool Slave;
            /// <summary>The pawn is a guest of the colony.</summary>
            public bool Guest;
            /// <summary>The pawn belongs to some faction at all.</summary>
            public bool HasFaction;
            /// <summary>The pawn's faction is hostile to the colony.</summary>
            public bool EnemyFaction;
            /// <summary>The mental state's name ("Berserk"), empty when sane.</summary>
            public string MentalState;
            /// <summary>The current job's defName ("AttackMelee"), empty for none.</summary>
            public string JobDef;
            /// <summary>The current job's target belongs to the colony.</summary>
            public bool JobTargetsColony;
            /// <summary>The lord job's class name (LordJob_AssaultColony), empty for none.</summary>
            public string LordJob;
        }

        /// <summary>
        /// The side a pawn is on, in the words the tool prints: "manhunter",
        /// "wild man", "prisoner", "slave", "guest", "hostile", "factionless"
        /// or "neutral". One label for the whole tool (fox ticket #3690): the
        /// fight list called a wild man "factionless", which says nothing about
        /// why he is a threat or that he can be arrested or tamed.
        /// </summary>
        public static string KindOf(PawnState pawn)
        {
            if (pawn.Animal && IsManhunterState(pawn.MentalState)) return "manhunter";
            if (pawn.WildMan) return "wild man";
            if (pawn.Prisoner) return "prisoner";
            if (pawn.Slave) return "slave";
            if (pawn.Guest) return "guest";
            if (!pawn.HasFaction) return "factionless";
            if (pawn.EnemyFaction) return "hostile";
            return "neutral";
        }

        /// <summary>
        /// True when the pawn is a threat the fight reads and the danger line
        /// must count. A colonist is never one: a berserk colonist stays in the
        /// colonists list and the digest names his mental state, so counting him
        /// here as well would report a hostile nobody lists.
        /// </summary>
        public static bool ThreatensColony(PawnState pawn)
        {
            if (pawn.Colonist) return false;
            if (pawn.EnemyFaction) return true;
            if (IsManhunterState(pawn.MentalState)) return true;
            if (IsAggroState(pawn.MentalState)) return true;
            if (IsAttackJob(pawn.JobDef) && pawn.JobTargetsColony) return true;
            return IsAssaultingLord(pawn.LordJob);
        }

        /// <summary>
        /// The states the game turns an animal into a hunter with. A manhunter
        /// warg was the case behind ticket #3451: the headcount was humanlikes
        /// only, so a raid of teeth reached the tool as "0 hostiles".
        /// </summary>
        public static bool IsManhunterState(string mentalState)
        {
            return mentalState == "Manhunter" || mentalState == "ManhunterPermanent";
        }

        /// <summary>
        /// A mental state in which the pawn comes for people rather than sitting
        /// somewhere: a guest who goes berserk is a threat although his faction
        /// is at peace. The peaceful states (Wander_Psychotic, PanicFlee, the
        /// binges) stay out.
        /// </summary>
        public static bool IsAggroState(string mentalState)
        {
            switch (mentalState)
            {
                case "Berserk":
                case "BerserkWander":
                case "RunAmok":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True for the game's attack jobs, which is what a pawn beating or
        /// shooting somebody is in. The name test catches the variants
        /// (AttackMelee, AttackStatic, AttackMeleeForced); a job that only
        /// happens to contain "Attack" costs a shorter wait, not a colonist.
        /// </summary>
        public static bool IsAttackJob(string jobDef)
        {
            if (string.IsNullOrEmpty(jobDef)) return false;
            if (jobDef == "AttackMelee" || jobDef == "AttackStatic") return true;
            return jobDef.IndexOf("Attack", System.StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// True for a lord job that attacks the colony. The game names nearly
        /// every assault after what it attacks (AssaultColony, AssaultThings,
        /// the anomaly's ChimeraAssault), so the name catches them; the few that
        /// do not carry "Assault" are listed. The same list is the tool's
        /// (core/rimworld/actions/waitstep.go, assaultingLord), so a group reads
        /// the same way in the fight block and in the wait's cap.
        /// </summary>
        public static bool IsAssaultingLord(string lordJob)
        {
            if (string.IsNullOrEmpty(lordJob)) return false;
            if (lordJob.IndexOf("Assault", System.StringComparison.Ordinal) >= 0) return true;
            switch (lordJob)
            {
                case "LordJob_Siege":
                case "LordJob_StageThenAttack":
                case "LordJob_ManTurrets":
                case "LordJob_Kidnap":
                case "LordJob_Steal":
                case "LordJob_PrisonBreak":
                case "LordJob_SlaveRebellion":
                case "LordJob_EntitySwarm":
                    return true;
            }
            return false;
        }
    }
}
