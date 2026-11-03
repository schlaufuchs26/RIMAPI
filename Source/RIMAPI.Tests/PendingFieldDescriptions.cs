using System;
using System.Collections.Generic;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The model areas whose fields carry no description yet (fox ticket
    /// #4313).
    /// </summary>
    /// <remarks>
    /// The gate in ParameterDescriptionTests fails for a field with no text,
    /// so the migration happens one area per run and this list is the ratchet:
    /// a wave describes its area's types and removes the area here. An area
    /// covers the model directories that belong to it (Models/Map and
    /// Models/World are map-world, Models/Pawns and Models/Combat are pawn,
    /// the root files are colony). Waves 1 and 2 (pawn, colony) emptied those areas. When
    /// the list is empty every field of every schema says what it means. Add
    /// an area here only to open a new migration, never to silence a field:
    /// a new type in a finished area fails the gate at once.
    /// </remarks>
    internal static class PendingFieldDescriptions
    {
        public static readonly HashSet<string> Areas = new HashSet<string>(
            new[] { "map-world", "system", "client", "other" },
            StringComparer.Ordinal
        );
    }
}
