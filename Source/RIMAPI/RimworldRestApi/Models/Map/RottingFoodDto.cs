using System.Collections.Generic;

namespace RIMAPI.Models
{
    /// <summary>
    /// The perishable food on the map that sits in a room above 0 C (fox
    /// ticket #3672). The room read names every room's temperature and the
    /// things read every stack, but neither says which food is in which room:
    /// colony 14 lost 59 horse meat to rot in a room a PassiveCooler held at
    /// 16 C, and the only warning was the game's own message after the loss.
    /// </summary>
    public class MapRottingFoodDto
    {
        public List<RottingFoodDto> Items { get; set; } = new List<RottingFoodDto>();
        public int TotalStacks { get; set; }
        public int TotalUnits { get; set; }
        /// <summary>The warmest room in the answer, so a caller can name the worst one without re-reading the rooms.</summary>
        public float WarmestTemperature { get; set; }
    }

    /// <summary>
    /// One stack of perishable food in a warm room. HoursUntilRot is the
    /// game's own estimate from the stack's accumulated rot progress, so a
    /// stack that has sat warm for days reads a small number even at a
    /// temperature where fresh food would last weeks. The value is clamped at
    /// 100000 hours: an item that would outlast any colony decision reads as
    /// "not soon" instead of an infinite JSON number.
    /// </summary>
    public class RottingFoodDto
    {
        public int ThingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public int StackCount { get; set; }
        public float Nutrition { get; set; }
        public float HoursUntilRot { get; set; }
        public int RoomId { get; set; }
        public string RoomLabel { get; set; }
        public float RoomTemperature { get; set; }
        public PositionDto Position { get; set; }
    }
}
