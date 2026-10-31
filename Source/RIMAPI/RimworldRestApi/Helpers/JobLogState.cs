namespace RIMAPI.Helpers
{
    // The job record's change rule (tickets #3300, #3392): a line is written
    // only when the pawn's state differs from the one recorded before, because
    // StartJob and EndCurrentJob both fire on one transition. Game-free, so the
    // unit tests pin it; JobLog passes the recorded and the current values.
    public static class JobLogState
    {
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
