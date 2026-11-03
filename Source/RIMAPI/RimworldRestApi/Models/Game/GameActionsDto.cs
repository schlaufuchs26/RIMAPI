using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class SendLetterRequestDto
    {
        public string Label { get; set; }
        [ParameterDescription("The text the letter shows the player.")]
        public string Message { get; set; }
        [ParameterDescription("The letter's def name.")]
        public string LetterDef { get; set; }
        public string MapId { get; set; }
        [ParameterDescription("The thing the letter points the camera at.")]
        public string LookTargetThingId { get; set; }
        [ParameterDescription("The faction the letter names, by its order id.")]
        public string FactionOrderId { get; set; }
        [ParameterDescription("The defs the letter links to.")]
        public List<string> HyperlinkThingDefs { get; set; }
        [ParameterDescription("The quest the letter belongs to.")]
        public string QuestId { get; set; }
        [ParameterDescription("Extra text the letter carries for debugging.")]
        public string DebugInfo { get; set; }
        [ParameterDescription("How many ticks to wait before showing it.")]
        public int DelayTicks { get; set; }
        [ParameterDescription("The sound the letter plays when it arrives.")]
        public bool PlaySound { get; set; }
    }
}
