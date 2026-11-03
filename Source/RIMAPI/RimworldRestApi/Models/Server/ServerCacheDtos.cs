using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ServerCacheResponseDto
    {
        [ParameterDescription("A sentence about what the call did.")]
        public string Message { get; set; }
    }
}
