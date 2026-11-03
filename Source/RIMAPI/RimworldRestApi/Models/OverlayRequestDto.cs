using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class OverlayRequestDto
    {
        [ParameterDescription("The text to draw on the screen.")]
        public string Text { get; set; }
        [ParameterDescription("How long the text stays, in seconds.")]
        public float Duration { get; set; } = 3.0f; // Seconds
        [ParameterDescription("The text colour, as the game names it.")]
        public string Color { get; set; } = "#FFFFFF"; // Hex code
        [ParameterDescription("How large the text is drawn, as a multiplier.")]
        public float Scale { get; set; } = 2.0f; // Text size multiplier
    }
}