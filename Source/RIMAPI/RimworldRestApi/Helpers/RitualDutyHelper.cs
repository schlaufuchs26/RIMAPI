using System.Collections.Generic;
using System.Linq;

namespace RIMAPI.Helpers
{
    // A running ritual only finishes while its pawns hold the duties its stages
    // hand out (the leader's speech: GiveSpeech for the speaker, Spectate for
    // the listeners). Once they are back on ordinary work the lord job keeps
    // standing and the game's own verdict stays "already in progress", so the
    // read needs the attendance check on its own (ticket #3364).
    //
    // Kept free of RimWorld types so it compiles in the unit test project: the
    // service maps the game's DutyDefs to their defNames and asks here.
    public static class RitualDutyHelper
    {
        // AnyoneOnDuty is true when one of the pawns still holds one of the
        // ritual's duties. An empty duty set cannot be judged, so it counts as
        // attended: the read then reports the pawns' jobs and claims nothing.
        public static bool AnyoneOnDuty(IEnumerable<string> pawnDuties, IEnumerable<string> ritualDuties)
        {
            if (pawnDuties == null || ritualDuties == null) return true;
            HashSet<string> wanted = new HashSet<string>(ritualDuties.Where(d => !string.IsNullOrEmpty(d)));
            if (wanted.Count == 0) return true;
            return pawnDuties.Any(d => !string.IsNullOrEmpty(d) && wanted.Contains(d));
        }
    }
}
