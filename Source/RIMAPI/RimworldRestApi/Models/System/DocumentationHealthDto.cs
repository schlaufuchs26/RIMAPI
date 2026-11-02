using System;
using System.Collections.Generic;

namespace RIMAPI.Models
{
    /// <summary>The documentation service's health answer (fox ticket #4304).</summary>
    /// <remarks>
    /// The route built an anonymous object before, so the spec could name no
    /// type for it and the schema stayed free-form.
    /// </remarks>
    public class DocumentationHealthDto
    {
        public string Status { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalEndpoints { get; set; }
        public int TotalExtensions { get; set; }
        public List<string> Sections { get; set; }
    }
}
