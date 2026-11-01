using System.Globalization;

namespace RIMAPI.Helpers
{
    // Why a powered building does not work, in one sentence (fox ticket #3762).
    // A stove four cells from the nearest conduit is placed, finished and dead,
    // and every figure the power read sums stays silent about it: no net holds
    // it, so its draw is in no total. The bill on that stove read "running"
    // for four game hours while the meals fell from 9 to 4.
    //
    // Game-free: the caller gathers the facts (the power comp's own answers and
    // the distance to the nearest conduit cell) and this decides the wording,
    // so the sentences are tested without the game.
    public static class PowerFault
    {
        public struct Facts
        {
            // Label is the building's name as the game shows it, e.g.
            // "electric stove"; the sentence puts "the" in front of it.
            public string Label;
            // ConsumesPower is true for a machine that draws power when it
            // runs. A producer or a battery never has this fault.
            public bool ConsumesPower;
            // SwitchedOn is the player's own switch (FlickUtility).
            public bool SwitchedOn;
            // Connected is true when a power net reaches the building at all;
            // Powered is the game's own answer to "does it have power now".
            public bool Connected;
            public bool Powered;
            // NetProduction and NetStored describe the net the building stands
            // on, in W and Wd.
            public float NetProduction;
            public float NetStored;
            // ConduitDistance is how many cells away the nearest conduit cell
            // stands, -1 when the map holds none; ConduitX/ConduitZ name that
            // cell.
            public int ConduitDistance;
            public int ConduitX;
            public int ConduitZ;
        }

        // Describe answers the sentence that says why the building has no
        // power, or null when it has power or does not need any. The order is
        // the order a player checks: the switch they flipped, the wire that
        // never reached it, the grid it reached and that carries nothing.
        public static string Describe(Facts f)
        {
            if (!f.ConsumesPower || f.Powered)
                return null;

            string who = "the " + (string.IsNullOrEmpty(f.Label) ? "building" : f.Label);
            if (!f.SwitchedOn)
            {
                return who + " is switched off";
            }
            if (!f.Connected)
            {
                return who + " is not connected to a power net: " + ConduitText(f);
            }
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} is connected, but its net produces {1} W and stores {2} Wd",
                who,
                Number(f.NetProduction),
                Number(f.NetStored));
        }

        // ConduitText names the nearest conduit cell, which is the distance a
        // player measures by eye before building one.
        private static string ConduitText(Facts f)
        {
            if (f.ConduitDistance < 0)
            {
                return "the map has no conduit at all";
            }
            string cells = f.ConduitDistance == 1 ? "cell" : "cells";
            return string.Format(
                CultureInfo.InvariantCulture,
                "the nearest conduit stands {0} {1} away at ({2},{3})",
                f.ConduitDistance,
                cells,
                f.ConduitX,
                f.ConduitZ);
        }

        // Number drops the decimal on a whole figure: "0 W", not "0.0 W".
        private static string Number(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
