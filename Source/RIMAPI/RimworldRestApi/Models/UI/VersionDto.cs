namespace RIMAPI.Models
{
    public class VersionDto
    {
        public string Version { get; set; }
        public string RimWorldVersion { get; set; }
        public string ModVersion { get; set; }
        public string ApiVersion { get; set; }
        // The folder this game writes its saves into (ticket #4005): a caller
        // that runs several games tells its own from another game that answers
        // on the same port by it, since the folder is what a game is.
        public string SaveDataFolder { get; set; }
    }
}
