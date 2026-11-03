using System.Collections.Generic;

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
        public int MarkerId { get; set; }

        /// <summary>The pen's label, the name the game gives it ("Pen 1").</summary>
        public string Label { get; set; }

        public string DefName { get; set; }

        public int X { get; set; }

        public int Z { get; set; }

        /// <summary>The game's verdict: the pen is closed by fences, a gate counts.</summary>
        public bool Enclosed { get; set; }

        /// <summary>True while the pen is not closed, so animals walk out of it.</summary>
        public bool Unenclosed { get; set; }

        /// <summary>The pen has an opening to the outside world (a gate is passable, that is normal).</summary>
        public bool HasOutsideAccess { get; set; }

        /// <summary>The pen's outline carries passable doors.</summary>
        public bool PassableDoors { get; set; }

        /// <summary>Cells the pen encloses (PenFoodCalculator.numCells).</summary>
        public int Cells { get; set; }

        /// <summary>Of those cells, the ones that grow food (numCellsSoil).</summary>
        public int SoilCells { get; set; }

        /// <summary>Nutrition the pen's own ground grows per day today, the tab's "natural growth".</summary>
        public float NutritionPerDay { get; set; }

        /// <summary>Nutrition the animals inside eat per day.</summary>
        public float ConsumptionPerDay { get; set; }

        /// <summary>Nutrition of the food stored inside the pen right now.</summary>
        public float StockpiledNutrition { get; set; }

        /// <summary>Per animal type: how many stand inside, what they eat, and the pen's capacity for that type.</summary>
        public List<PenAnimalDto> Animals { get; set; }

        /// <summary>Every animal inside the pen by name, so a run can name one.</summary>
        public List<PenAnimalMemberDto> Members { get; set; }

        /// <summary>The food stored inside the pen, per item type.</summary>
        public List<PenFoodDto> Stockpiled { get; set; }

        /// <summary>The Auto-cut tab's checkbox: the pen cuts allowed plants by itself (fox ticket #4316).</summary>
        public bool AutoCut { get; set; }

        /// <summary>The animal defNames the pen's Animals tab takes, the filter the write route sets.</summary>
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
        public float NutritionPerDay { get; set; }

        /// <summary>
        /// How many of this type the pen can feed: the game's own number for the
        /// pen tab's animal capacity (PenFoodCalculator.CapacityOf), so a caller
        /// never has to guess from the cell count.
        /// </summary>
        public float Capacity { get; set; }
    }

    /// <summary>One animal standing inside a pen.</summary>
    public class PenAnimalMemberDto
    {
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

        public float Nutrition { get; set; }
    }
}
