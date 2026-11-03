using System;
using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>The documentation service's health answer (fox ticket #4304).</summary>
    /// <remarks>
    /// The route built an anonymous object before, so the spec could name no
    /// type for it and the schema stayed free-form.
    /// </remarks>
    public class DocumentationHealthDto
    {
        [ParameterDescription("Whether the document is healthy.")]
        public string Status { get; set; }
        [ParameterDescription("When the document check ran.")]
        public DateTime GeneratedAt { get; set; }
        [ParameterDescription("How many routes the document lists.")]
        public int TotalEndpoints { get; set; }
        [ParameterDescription("How many extensions it carries.")]
        public int TotalExtensions { get; set; }
        [ParameterDescription("One entry per document section.")]
        public List<string> Sections { get; set; }
    }
}
