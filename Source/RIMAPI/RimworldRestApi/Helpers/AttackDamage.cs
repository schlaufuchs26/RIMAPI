namespace RIMAPI.Helpers
{
    /// <summary>
    /// What one attack of a pawn takes off a target (fox ticket #3451). The
    /// combat read named a weapon and its range but no damage, so a run could
    /// not see that standing and trading blows with a warg was a loss.
    ///
    /// The rule is small enough to state without a game: a verb whose range is
    /// above melee reach is a ranged attack and its damage is the projectile's;
    /// anything else is a blow and its damage is the melee amount. A verb
    /// without damage (a tool that only stuns, an ability) reads zero, and the
    /// tool then says nothing rather than "0 damage".
    /// </summary>
    public static class AttackDamage
    {
        /// <summary>
        /// The game's own cut between a blow and a shot: a melee verb reaches
        /// 1.5 cells, so anything longer is ranged (the same threshold the
        /// tool uses to tell whether a pawn can shoot).
        /// </summary>
        public const float MeleeReach = 1.6f;

        /// <summary>One attack's damage, and which kind it is.</summary>
        public struct Hit
        {
            public float Damage;
            public bool Ranged;

            /// <summary>"melee" or "ranged", the word the read prints.</summary>
            public string Kind => Ranged ? "ranged" : "melee";
        }

        public static Hit Of(float range, float meleeDamage, float projectileDamage)
        {
            if (range > MeleeReach)
            {
                return new Hit { Damage = projectileDamage, Ranged = true };
            }

            return new Hit { Damage = meleeDamage, Ranged = false };
        }
    }
}
