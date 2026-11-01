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
            if (facts.Range > 0f && facts.Distance > facts.Range)
            {
                return $"out of range: {Cells(facts.Distance)} away, the weapon reaches {Cells(facts.Range)}";
            }
            if (facts.MinRange > 0f && facts.Distance < facts.MinRange)
            {
                return $"too close: {Cells(facts.Distance)} away, the minimum is {Cells(facts.MinRange)}";
            }
            return "no line of sight from where he stands";
        }

        private static string Cells(float cells)
        {
            return cells.ToString("0.#") + (cells == 1f ? " cell" : " cells");
        }
    }
}
