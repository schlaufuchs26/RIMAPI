using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models.Map
{
    // The animal pen's own controls (fox ticket #4316, parity wave 8): the
    // Animals tab's filter, the Auto-cut tab's checkbox and button, and the
    // marker's name. Each request names the pen by its marker_id, the id the
    // pens read prints.

    /// <summary>Which animal kinds the pen takes: defNames or categories to allow or deny.</summary>
    public class PenAnimalsRequestDto
    {
        [ParameterDescription("The map the pen stands on, as the map list numbers them.")]
        public int MapId { get; set; }

        [ParameterDescription("The pen marker's thing id, as the pens read prints its marker_id.")]
        public int MarkerId { get; set; }

        [ParameterDescription("Animal defNames or categories the pen should take, e.g. Muffalo.")]
        public List<string> Allow { get; set; }

        [ParameterDescription("Animal defNames or categories the pen should refuse, e.g. Chicken.")]
        public List<string> Deny { get; set; }
    }

    /// <summary>The Auto-cut tab: the checkbox and the "Auto-cut now" button.</summary>
    public class PenAutoCutRequestDto
    {
        [ParameterDescription("The map the pen stands on, as the map list numbers them.")]
        public int MapId { get; set; }

        [ParameterDescription("The pen marker's thing id, as the pens read prints its marker_id.")]
        public int MarkerId { get; set; }

        [ParameterDescription("The checkbox: whether the pen cuts allowed plants by itself.")]
        public bool? Enabled { get; set; }

        [ParameterDescription("The button: mark every allowed plant in the pen for cutting now.")]
        public bool CutNow { get; set; }
    }

    /// <summary>The marker's rename, under the rename dialog's rules.</summary>
    public class PenRenameRequestDto
    {
        [ParameterDescription("The map the pen stands on, as the map list numbers them.")]
        public int MapId { get; set; }

        [ParameterDescription("The pen marker's thing id, as the pens read prints its marker_id.")]
        public int MarkerId { get; set; }

        [ParameterDescription("The new name, at most 28 characters like the dialog's field.")]
        public string Label { get; set; }
    }

    /// <summary>
    /// One pen after a write: its name, whether auto-cut is on, and the animal
    /// kinds it takes now. A write that names defs or categories the game's own
    /// filter does not carry answers them in unknown instead of dropping them.
    /// </summary>
    public class PenEditResultDto
    {
        [ParameterDescription("The pen marker's thing id, as the pens read prints its marker_id.")]
        public int MarkerId { get; set; }

        [ParameterDescription("The pen's name after the write, as the marker shows it.")]
        public string Label { get; set; }

        [ParameterDescription("Whether the pen cuts allowed plants by itself.")]
        public bool AutoCut { get; set; }

        [ParameterDescription("True when this call marked plants for cutting now.")]
        public bool CutMarked { get; set; }

        [ParameterDescription("The animal defNames the pen takes after the write.")]
        public List<string> AnimalsAllowed { get; set; }

        [ParameterDescription("The defNames this call applied to the pen's filter.")]
        public List<string> AppliedDefs { get; set; }

        [ParameterDescription("The categories this call applied to the pen's filter.")]
        public List<string> AppliedCategories { get; set; }

        [ParameterDescription("Names that matched neither a def nor a category of the pen's filter.")]
        public List<string> Unknown { get; set; }
    }
}
