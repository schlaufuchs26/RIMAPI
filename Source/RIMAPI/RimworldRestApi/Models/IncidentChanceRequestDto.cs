using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class IncidentChanceRequestDto
    {
        [ParameterDescription("The incident's def name to read the chance of.")]
        public string IncidentDefName { get; set; }
    }
}
