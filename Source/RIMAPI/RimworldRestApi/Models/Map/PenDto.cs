using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// One animal pen on the map: the marker's cell, what the game computes as
    /// its enclosure, the animals standing inside and the food numbers the
    /// marker's own pen tab shows. A pen could be built through the bridge but
    /// never read, so a run could not tell whether a second pen was needed
    /// (fox ticket #3239).
    /// </summary>
    public class PenDto
    {
        /// <summary>The marker building's thing id, the id the things read prints.</summary>
        [ParameterDescription("The marker building's thing id, the id the things read prints.")]
        public int MarkerId { get; set; }

        /// <summary>The pen's label, the name the game gives it ("Pen 1").</summary>
        public string Label { get; set; }

        public string DefName { get; set; }

        public int X { get; set; }

        public int Z { get; set; }

        /// <summary>The game's verdict: the pen is closed by fences, a gate counts.</summary>
        [ParameterDescription("The game's verdict: the pen is closed by fences, a gate counts.")]
        public bool Enclosed { get; set; }

        /// <summary>True while the pen is not closed, so animals walk out of it.</summary>
        [ParameterDescription("True while the pen is not closed, so animals walk out of it.")]
        public bool Unenclosed { get; set; }

        /// <summary>The pen has an opening to the outside world (a gate is passable, that is normal).</summary>
        [ParameterDescription("The pen has an opening to the outside world (a gate is passable, that is normal).")]
        public bool HasOutsideAccess { get; set; }

        /// <summary>The pen's outline carries passable doors.</summary>
        [ParameterDescription("The pen's outline carries passable doors.")]
        public bool PassableDoors { get; set; }

        /// <summary>Cells the pen encloses (PenFoodCalculator.numCells).</summary>
        [ParameterDescription("Cells the pen encloses (PenFoodCalculator.numCells).")]
        public int Cells { get; set; }

        /// <summary>Of those cells, the ones that grow food (numCellsSoil).</summary>
        [ParameterDescription("Of those cells, the ones that grow food (numCellsSoil).")]
        public int SoilCells { get; set; }

        /// <summary>Nutrition the pen's own ground grows per day today, the tab's "natural growth".</summary>
        [ParameterDescription("Nutrition the pen's own ground grows per day today, the tab's \"natural growth\".")]
        public float NutritionPerDay { get; set; }

        /// <summary>Nutrition the animals inside eat per day.</summary>
        [ParameterDescription("Nutrition the animals inside eat per day.")]
        public float ConsumptionPerDay { get; set; }

        /// <summary>Nutrition of the food stored inside the pen right now.</summary>
        [ParameterDescription("Nutrition of the food stored inside the pen right now.")]
        public float StockpiledNutrition { get; set; }

        /// <summary>Per animal type: how many stand inside, what they eat, and the pen's capacity for that type.</summary>
        [ParameterDescription("Per animal type: how many stand inside, what they eat, and the pen's capacity for that type.")]
        public List<PenAnimalDto> Animals { get; set; }

        /// <summary>Every animal inside the pen by name, so a run can name one.</summary>
        [ParameterDescription("Every animal inside the pen by name, so a run can name one.")]
        public List<PenAnimalMemberDto> Members { get; set; }

        /// <summary>The food stored inside the pen, per item type.</summary>
        [ParameterDescription("The food stored inside the pen, per item type.")]
        public List<PenFoodDto> Stockpiled { get; set; }

        /// <summary>The Auto-cut tab's checkbox: the pen cuts allowed plants by itself (fox ticket #4316).</summary>
        [ParameterDescription("The Auto-cut tab's checkbox: the pen cuts allowed plants by itself (fox ticket #4316).")]
        public bool AutoCut { get; set; }

        /// <summary>The animal defNames the pen's Animals tab takes, the filter the write route sets.</summary>
        [ParameterDescription("The animal defNames the pen's Animals tab takes, the filter the write route sets.")]
        public List<string> AnimalsAllowed { get; set; }
    }

    /// <summary>One animal type in a pen, with the game's own capacity for it.</summary>
    public class PenAnimalDto
    {
        public string DefName { get; set; }

        public string Label { get; set; }

        /// <summary>How many of this type stand inside the pen.</summary>
        public int Count { get; set; }

        /// <summary>Nutrition this type eats per day in total (count times one animal).</summary>
        [ParameterDescription("Nutrition this type eats per day in total (count times one animal).")]
        public float NutritionPerDay { get; set; }

        /// <summary>
        /// How many of this type the pen can feed: the game's own number for the
        /// pen tab's animal capacity (PenFoodCalculator.CapacityOf), so a caller
        /// never has to guess from the cell count.
        /// </summary>
        [ParameterDescription("How many of this type the pen can feed: the game's own number for the pen tab's animal capacity (PenFoodCalculator.CapacityOf), so a caller never has to guess from the cell count.")]
        public float Capacity { get; set; }
    }

    /// <summary>One animal standing inside a pen.</summary>
    public class PenAnimalMemberDto
    {
        [ParameterDescription("The animal's numeric id.")]
        public int Id { get; set; }

        public string Name { get; set; }

        public string DefName { get; set; }
    }

    /// <summary>One food item type stored inside a pen.</summary>
    public class PenFoodDto
    {
        public string DefName { get; set; }

        public string Label { get; set; }

        public int Count { get; set; }

        [ParameterDescription("How much nutrition the stack holds.")]
        public float Nutrition { get; set; }
    }
}
