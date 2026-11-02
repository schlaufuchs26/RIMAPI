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
    /// [ResponseBody], so the migration happened one area per run and this list
    /// was the ratchet. The last wave emptied it: every route of every area now
    /// declares the type it answers with, and a new route that forgets the
    /// declaration fails the gate wherever it lands. Add an area here only to
    /// open a new migration, never to silence a route.
    /// </remarks>
    internal static class PendingResponseSchemas
    {
        public static readonly HashSet<string> Areas = new HashSet<string>(
            new string[0],
            StringComparer.Ordinal
        );
    }
}
