using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class VersionDto
    {
        [ParameterDescription("The mod's version, as its About file names it.")]
        public string Version { get; set; }
        [ParameterDescription("The game version the mod runs on.")]
        public string RimWorldVersion { get; set; }
        [ParameterDescription("The mod's assembly version.")]
        public string ModVersion { get; set; }
        [ParameterDescription("The API version the routes answer with.")]
        public string ApiVersion { get; set; }
        // The folder this game writes its saves into (ticket #4005): a caller
        // that runs several games tells its own from another game that answers
        // on the same port by it, since the folder is what a game is.
        [ParameterDescription("The folder this game writes its saves into (ticket #4005): a caller that runs several games tells its own from another game that answers on the same port by it, since the folder is what a game is.")]
        public string SaveDataFolder { get; set; }
    }
}
