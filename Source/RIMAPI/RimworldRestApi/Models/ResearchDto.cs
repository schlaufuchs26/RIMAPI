using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ResearchFinishedDto
    {
        [ParameterDescription("The projects the colony has finished.")]
        public List<string> FinishedProjects { get; set; } = new List<string>();
    }

    public class ResearchTreeDto
    {
        [ParameterDescription("One entry per research project.")]
        public List<ResearchProjectDto> Projects { get; set; } = new List<ResearchProjectDto>();
    }

    public class ResearchProjectDto
    {
        public string Name { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How far the project has come, 0 to 1.")]
        public int Progress { get; set; }
        [ParameterDescription("How many research points the project costs.")]
        public int ResearchPoints { get; set; }
        public string Description { get; set; }
        [ParameterDescription("Whether the colony has finished it.")]
        public bool IsFinished { get; set; }
        [ParameterDescription("Whether the project can be started now.")]
        public bool CanStartNow { get; set; }
        [ParameterDescription("Whether the colony has a bench the project needs.")]
        public bool PlayerHasAnyAppropriateResearchBench { get; set; }
        [ParameterDescription("How many things must be analysed first.")]
        public int RequiredAnalyzedThingCount { get; set; }
        [ParameterDescription("How many of those analyses are done.")]
        public int AnalyzedThingsCompleted { get; set; }
        [ParameterDescription("The tech level the project belongs to.")]
        public string TechLevel { get; set; }

        [ParameterDescription("The projects that must finish first.")]
        public List<string> Prerequisites { get; set; } = new List<string>();
        [ParameterDescription("The prerequisites the game does not show.")]
        public List<string> HiddenPrerequisites { get; set; } = new List<string>();
        [ParameterDescription("The projects this one is a prerequisite for.")]
        public List<string> RequiredByThis { get; set; } = new List<string>();
        [ParameterDescription("The progress in percent, 0 to 100.")]
        public float ProgressPercent { get; set; }
    }

    public class ResearchSummaryDto
    {
        [ParameterDescription("How many projects the colony has finished.")]
        public int FinishedProjectsCount { get; set; }
        [ParameterDescription("How many projects the game holds.")]
        public int TotalProjectsCount { get; set; }
        [ParameterDescription("How many projects can be started now.")]
        public int AvailableProjectsCount { get; set; }
        [ParameterDescription("The projects grouped by tech level.")]
        public Dictionary<string, ResearchCategoryDto> ByTechLevel { get; set; }
        [ParameterDescription("The projects grouped by research tab.")]
        public Dictionary<string, ResearchCategoryDto> ByTab { get; set; }
    }

    public class ResearchCategoryDto
    {
        [ParameterDescription("How many projects in the group are finished.")]
        public int Finished { get; set; }
        [ParameterDescription("How many projects the group holds.")]
        public int Total { get; set; }
        public float PercentComplete => Total > 0 ? Finished / (float)Total * 100 : 0;
        [ParameterDescription("One entry per project in the group.")]
        public List<string> Projects { get; set; }
    }
}
