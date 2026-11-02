using System;
using System.Collections.Generic;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The areas whose routes do not declare a response type yet (fox ticket
    /// #4304).
    /// </summary>
    /// <remarks>
    /// The gate in ResponseSchemaTests fails for a route that declares no
    /// [ResponseBody], so the migration happens one area per run and this list
    /// is the ratchet: an area leaves it in the run that migrates it, and the
    /// test then fails for any later route in that area that forgets its
    /// declaration. The last wave empties the list.
    ///
    /// Waves, in the order the ticket names them: pawn, colony, map-world,
    /// system, client, dev, ai, other (the docs and cache routes of
    /// BaseControllers). The pawn wave is done and left the list; the rest
    /// follow one run each.
    /// </remarks>
    internal static class PendingResponseSchemas
    {
        public static readonly HashSet<string> Areas = new HashSet<string>(
            new[] { "colony", "map-world", "system", "client", "dev", "ai", "other" },
            StringComparer.Ordinal
        );
    }
}
