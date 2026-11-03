
using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models.UI
{
    public class AlertDto
    {
        public string Label { get; set; }
        [ParameterDescription("The explanation the alert shows the player.")]
        public string Explanation { get; set; }

        // e.g., "Critical", "High", "Medium"
        [ParameterDescription("e.g., \"Critical\", \"High\", \"Medium\"")]
        public string Priority { get; set; }
        [ParameterDescription("The things the alert points at.")]
        public List<int> Targets { get; set; }
        [ParameterDescription("The cells the alert points at.")]
        public List<string> Cells { get; set; }
    }
}