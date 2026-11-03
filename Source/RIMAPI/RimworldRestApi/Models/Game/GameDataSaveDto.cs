using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class GameLoadRequestDto
    {
        [ParameterDescription("The save to load, by name.")]
        public string FileName { get; set; }
        [ParameterDescription("Whether to check the save's version first.")]
        public bool CheckVersion { get; set; }
        [ParameterDescription("Whether to load despite a mod mismatch.")]
        public bool SkipModMismatch { get; set; }
    }

    public class GameSaveRequestDto
    {
        [ParameterDescription("The name to save under.")]
        public string FileName { get; set; }
    }
}
