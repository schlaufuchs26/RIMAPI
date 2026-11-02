using System;

namespace RIMAPI.Helpers
{
    // Which of a kill tale's two pawn slots holds the victim (fox ticket
    // #4014). The game's kill tales keep the killer in firstPawnData and the
    // victim in secondPawnData, except KilledBy (Tale_DoublePawnKilledBy),
    // which records the victim first: colony 17's save had the dead Fury first
    // and her living killer Pinky second, so the read named a standing
    // colonist as dead.
    //
    // The tale's def declares the slots (firstPawnSymbol VICTIM,
    // secondPawnSymbol KILLER), which is the authoritative answer. A tale def
    // from a mod may carry no symbols, so the pawns settle it: the victim of a
    // kill is dead and the killer usually is not.
    //
    // This file stays free of game types so the test project can compile it
    // (Source/RIMAPI.Tests).
    public static class KillTaleOrder
    {
        private const string Victim = "VICTIM";
        private const string Killer = "KILLER";

        // NamesVictimFirst answers whether firstPawnData is the victim and
        // secondPawnData the killer. The declared symbols win over the pawns:
        // two dead pawns (a killer who died later, a mutual kill) still leave
        // the order the def names. Without symbols the first pawn being dead
        // while the second is not is the one sign of the reversed order.
        public static bool NamesVictimFirst(string firstSymbol, string secondSymbol, bool firstDead, bool secondDead)
        {
            if (Is(firstSymbol, Victim) || Is(secondSymbol, Killer))
                return true;
            if (Is(secondSymbol, Victim) || Is(firstSymbol, Killer))
                return false;
            return firstDead && !secondDead;
        }

        private static bool Is(string symbol, string want)
        {
            return string.Equals(symbol, want, StringComparison.OrdinalIgnoreCase);
        }
    }
}
