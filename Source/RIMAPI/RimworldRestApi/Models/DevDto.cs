using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class DebugConsoleRequest
    {
        [ParameterDescription("The console command to run, e.g. destroy.")]
        public string Action { get; set; }
        [ParameterDescription("The text the command writes into the console.")]
        public string Message { get; set; }
    }

    public class StuffColorRequest
    {
        public string Name { get; set; }
        [ParameterDescription("The colour to give the material, as a hex string.")]
        public string Hex { get; set; }
    }

    public class MaterialsAtlasList
    {
        [ParameterDescription("One entry per material the atlas holds.")]
        public List<string> Materials { get; set; }
    }

    public class EndpointDto
    {
        [ParameterDescription("The HTTP method the route answers.")]
        public string Method { get; set; }
        [ParameterDescription("The route's path, as the registry holds it.")]
        public string Path { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The controller the route belongs to.")]
        public string Category { get; set; }
        [ParameterDescription("The tags the document groups the route under.")]
        public string[] Tags { get; set; }
        [ParameterDescription("Whether the route is marked deprecated.")]
        public bool IsDeprecated { get; set; }
    }

    public class EndpointListDto
    {
        [ParameterDescription("One entry per registered route.")]
        public List<EndpointDto> Endpoints { get; set; }
    }
}
