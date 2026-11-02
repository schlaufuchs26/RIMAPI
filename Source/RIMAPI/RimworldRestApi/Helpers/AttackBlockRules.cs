namespace RIMAPI.Helpers
{
    // Why an ordered attack does not land (fox ticket #3737). Colony 14's two
    // colonists were drafted and ordered onto a mad raccoon: the read-back said
    // "Job AttackMelee" and "Job AttackStatic", both took hits for an hour, and
    // the animal never lost a hit point. The tool could not tell "the shot
    // missed" from "the order never fired", and a colony with a gunner still
    // lost the fight.
    //
    // The game refuses a shot for four reasons, and each is a different next
    // step: a pawn the game disables violence for never fires, a pawn without a
    // verb has nothing to fire with, a target out of the weapon's range needs a
    // walk first, and a target without a line of sight needs a different cell.
    // The wording lives here so Source/RIMAPI.Tests can state it without a game;
    // CombatService fills the facts from the pawn and its verb.
    //
    // The second half is the choice of job (fox ticket #4160). Colony 17 ordered
    // a gunner onto a manhunting squirrel one cell away: a projectile weapon
    // cannot be fired at an adjacent target, so the order queued the ranged job,
    // the pawn stood beside the squirrel for ten minutes and neither side did
    // damage, while the read named the job AttackStatic (the game's job for
    // shooting a building). The game's own float menu offers "Melee attack"
    // exactly when the melee verb can reach, so an order now takes the same
    // choice, and every reason names the two cells it measured, because the
    // distance alone read as a contradiction with the map.
    public static class AttackBlockRules
    {
        /// <summary>One ordered attack, as the game reports it.</summary>
        public struct Facts
        {
            /// <summary>The game does not disable the violent work tag for him.</summary>
            public bool CanFight;
            /// <summary>The pawn has a verb to attack with (a weapon or its own body).</summary>
            public bool HasVerb;
            /// <summary>The game's own check for this verb from this cell.</summary>
            public bool CanHit;
            /// <summary>Cells between the pawn and the target.</summary>
            public float Distance;
            /// <summary>The verb's range in cells (EffectiveRange).</summary>
            public float Range;
            /// <summary>The verb's minimum range in cells; zero for most weapons.</summary>
            public float MinRange;
            /// <summary>The verb is a blow, so reach is a neighbouring cell, not a number.</summary>
            public bool IsMelee;
            /// <summary>The pawn carries a ranged weapon, whatever verb was read.</summary>
            public bool HasRanged;
            /// <summary>The pawn's melee verb reaches the target from where it stands.</summary>
            public bool MeleeCanHit;
            /// <summary>The cell the check measured from, e.g. "120,88"; empty when unread.</summary>
            public string From;
            /// <summary>The target's cell, e.g. "122,89"; empty when unread.</summary>
            public string Target;
        }

        /// <summary>Which of the game's two attack jobs an order queues.</summary>
        public enum AttackJob
        {
            /// <summary>JobDefOf.AttackMelee: the pawn closes and swings.</summary>
            Melee,
            /// <summary>JobDefOf.AttackStatic: the pawn stands and shoots.</summary>
            Ranged,
        }

        /// <summary>
        /// The job an ordered attack queues, the same way the game's own float
        /// menu picks between its two options: the ranged job while the weapon
        /// can hit from where the pawn stands, the melee job when only a blow
        /// reaches, and the ranged job when nothing can land (the note then
        /// says why). A pawn without a ranged weapon always fights in contact.
        /// </summary>
        public static AttackJob Choose(Facts facts)
        {
            if (!facts.HasRanged) return AttackJob.Melee;
            if (facts.CanHit) return AttackJob.Ranged;
            if (facts.MeleeCanHit) return AttackJob.Melee;
            return AttackJob.Ranged;
        }

        /// <summary>
        /// The reason the attack cannot land, or an empty string when it can.
        /// The order of the checks is the one a run has to act on: a pawn who
        /// cannot fight is no use whatever the distance, and a target inside
        /// the minimum range is a different problem from one beyond the range.
        /// </summary>
        public static string Reason(Facts facts)
        {
            if (!facts.CanFight) return "the game disables violence for this pawn, so he never fires";
            if (!facts.HasVerb) return "the pawn has no attack verb: neither a weapon nor a body that fights";
            if (facts.CanHit) return "";
            if (facts.IsMelee)
            {
                return $"out of reach: the target is {Cells(facts.Distance)} away and a blow needs a neighbouring cell"
                    + Where(facts);
            }
            if (facts.Range > 0f && facts.Distance > facts.Range)
            {
                return $"out of range: {Cells(facts.Distance)} away, the weapon reaches {Cells(facts.Range)}"
                    + Where(facts);
            }
            if (facts.MinRange > 0f && facts.Distance < facts.MinRange)
            {
                return $"too close: {Cells(facts.Distance)} away, the minimum is {Cells(facts.MinRange)}"
                    + Where(facts);
            }
            return "no line of sight" + (Where(facts).Length > 0 ? Where(facts) : " from where he stands");
        }

        /// <summary>
        /// The note for an order that swings instead of shooting: the weapon's
        /// own reason, then what the pawn does about it.
        /// </summary>
        public static string MeleeNote(Facts facts)
        {
            string reason = Reason(facts);
            if (reason.Length == 0) return "";
            return reason + ", so he fights in contact instead of shooting";
        }

        private static string Where(Facts facts)
        {
            if (string.IsNullOrEmpty(facts.From) || string.IsNullOrEmpty(facts.Target)) return "";
            return $" (he stands at {facts.From}, the target at {facts.Target})";
        }

        private static string Cells(float cells)
        {
            // Two decimals: a diagonal neighbour is 1.41 cells away and a
            // projectile's minimum range is 1.42, and one decimal rounds both to
            // "1.4", which reads as a check that contradicts itself.
            return cells.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                + (cells == 1f ? " cell" : " cells");
        }
    }
}
