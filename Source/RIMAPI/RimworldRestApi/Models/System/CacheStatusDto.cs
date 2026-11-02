using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>The cache status answer (fox ticket #4304).</summary>
    /// <remarks>
    /// The route built an anonymous object before, so the spec could name no
    /// type for it and the schema stayed free-form.
    /// </remarks>
    public class CacheStatusDto
    {
        public bool Enabled { get; set; }
        public CacheStatistics Statistics { get; set; }
    }
}
