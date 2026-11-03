using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class LearningConceptDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The text the learning helper shows for it.")]
        public string HelpText { get; set; }
        [ParameterDescription("How much of it the player has learned, 0 to 1.")]
        public float KnowledgeProgress { get; set; }
        [ParameterDescription("Whether the player has learned it fully.")]
        public bool IsCompleted { get; set; }
    }

    public class LearningConceptsListDto
    {
        [ParameterDescription("One entry per learning concept.")]
        public List<string> Defs { get; set; }
    }

    public class LearningConceptMarkDto
    {
        public string DefName { get; set; }
        [ParameterDescription("Whether the concept now counts as learned.")]
        public bool IsCompleted { get; set; }
    }
}