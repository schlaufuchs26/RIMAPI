using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class PawnInventoryDto
    {
        [ParameterDescription("The items the pawn carries in its inventory.")]
        public List<ThingDto> Items { get; set; }
        [ParameterDescription("The apparel the pawn is wearing.")]
        public List<ThingDto> Apparels { get; set; }
        [ParameterDescription("The weapons and tools the pawn has equipped.")]
        public List<ThingDto> Equipment { get; set; }
    }
}