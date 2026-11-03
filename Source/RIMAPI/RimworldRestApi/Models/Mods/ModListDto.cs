

using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ConfigureModsRequestDto
    {
        [ParameterDescription("The mods to switch on, by package id.")]
        public List<string> PackageIds { get; set; }
        [ParameterDescription("Whether the game restarts afterwards.")]
        public bool RestartGame { get; set; } = false;
    }

    public class ModInfoDto
    {
        public string Name { get; set; }
        [ParameterDescription("The mod's package id.")]
        public string PackageId { get; set; }
        [ParameterDescription("The mod's place in the load order.")]
        public int LoadOrder { get; set; }
        [ParameterDescription("The author the mod's About file names.")]
        public string Author { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The mod's home page.")]
        public string Url { get; set; }
        [ParameterDescription("The version the mod's About file names.")]
        public string Version { get; set; }
        [ParameterDescription("The game versions the mod supports.")]
        public List<string> SupportedVersions { get; set; }
        [ParameterDescription("The folder the mod lives in.")]
        public string RootDir { get; set; }
    }
}
