using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class SpawnDropPodRequestDto
    {
        public int MapId { get; set; }
        public PositionDto Position { get; set; }
        [ParameterDescription("The item stacks the pod carries.")]
        public List<ThingDto> Items { get; set; }
        [ParameterDescription("The faction the pod belongs to.")]
        public string Faction { get; set; } // Optional: "PlayerColony", "Ancients", etc.
        [ParameterDescription("How long the pod takes to open, in ticks.")]
        public bool OpenDelay { get; set; } = true; // Delay opening slightly for effect
    }
}