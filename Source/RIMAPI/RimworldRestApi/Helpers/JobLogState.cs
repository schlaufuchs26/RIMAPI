namespace RIMAPI.Helpers
{
    // The job record's change rule (tickets #3300, #3392): a line is written
    // only when the pawn's state differs from the one recorded before, because
    // StartJob and EndCurrentJob both fire on one transition. Game-free, so the
    // unit tests pin it; JobLog passes the recorded and the current values.
    public static class JobLogState
    {
        // Tracked says whether the record follows a pawn at all: the colony's
        // own, whether or not he is in a mental state. The game's
        // IsColonistPlayerControlled also fails for a colonist in a tantrum or
        // a berserk, so the record used to drop him exactly then (ticket
        // #3505); the rule is the one CombatRefusal uses since #3433.
        public static bool Tracked(bool colonist, bool playerFaction)
        {
            return colonist && playerFaction;
        }

        // Same says the recorded state and the current one are the same, so the
        // repeated hook writes nothing. The idle flag is part of the comparison:
        // the game can take the idle tag off without changing the job def, and
        // the record has to follow that (ticket #3392).
        public static bool Same(string lastJob, string lastTarget, bool lastIdle,
            string job, string target, bool idle)
        {
            return lastJob == job
                && lastTarget == target
                && lastIdle == idle;
        }
    }
}
