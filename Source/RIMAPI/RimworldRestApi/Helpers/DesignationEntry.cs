using RIMAPI.Models;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// One designation as the API answers it (fox ticket #3060). Both the
    /// designations read and the undesignate answer send this shape, so a
    /// caller sees what it removed in the same fields it read before the call
    /// (ticket #3508).
    ///
    /// Plain values, so the mapping is checked without a game: a designation
    /// hangs on a cell (mine, a floor to remove) or on a thing (a tree, a
    /// building, an animal).
    /// </summary>
    public static class DesignationEntry
    {
        // manhunterOnDamageChance is the hunted animal's race chance to turn
        // manhunter when it is hurt (fox #4183). It travels with the hunt
        // entry, because a run that marks prey has to see which of the marked
        // animals can turn on the hunter; every other order leaves it null.
        public static DesignationDto Make(
            string defName, string label, bool hasThing, int x, int z,
            int thingId = 0, string thingDefName = null, string thingLabel = null,
            float? manhunterOnDamageChance = null)
        {
            var dto = new DesignationDto
            {
                DefName = defName,
                Label = label,
                Target = hasThing ? "thing" : "cell",
                X = x,
                Z = z,
            };
            if (hasThing)
            {
                dto.ThingId = thingId;
                dto.ThingDefName = thingDefName;
                dto.ThingLabel = thingLabel;
                dto.ManhunterOnDamageChance = manhunterOnDamageChance;
            }
            return dto;
        }
    }
}
