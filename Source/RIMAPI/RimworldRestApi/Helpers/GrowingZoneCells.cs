using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// One growing zone's cell counts (fox ticket #3277), kept off the zone's
    /// own crop (fox ticket #3805).
    ///
    /// The sown count used to be every plant standing in the zone, so a wild
    /// tall grass that grew into a rice field made the field read one cell
    /// fuller than its crop was: "88 cells, Plant_Rice, 84 sown, 4 unsown" for
    /// 83 rice and one grass. A cell holding another plant can also not be
    /// sown while that plant stands, so it is neither sown nor empty. It gets
    /// its own count and names its plant instead.
    ///
    /// The counts add up: SownCells + UnsownCells + ForeignCells is the number
    /// of cells the caller walked.
    /// </summary>
    public class GrowingZoneCells
    {
        private readonly string _crop;
        private readonly List<string> _foreignPlants = new List<string>();

        public GrowingZoneCells(string cropDefName)
        {
            _crop = cropDefName;
        }

        public int SownCells { get; private set; }
        public int UnsownCells { get; private set; }
        public int ForeignCells { get; private set; }

        // The def names of the plants that are not the crop, one entry each, so
        // the read can say what stands in the field.
        public List<string> ForeignPlants
        {
            get { return _foreignPlants; }
        }

        public int DyingCells { get; private set; }
        public int BlightedCells { get; private set; }
        public int HarvestableCells { get; private set; }
        public int MatureCells { get; private set; }
        public float MaxGrowth { get; private set; }

        /// <summary>
        /// A cell without a plant: no soil, out of season, or a dead plant
        /// that was removed.
        /// </summary>
        public void AddEmpty()
        {
            UnsownCells++;
        }

        /// <summary>
        /// A cell with a plant standing on it. A crop plant counts as sown and
        /// feeds every other count; anything else is a foreign plant. With no
        /// crop set (a zone whose plant is None) there is nothing to compare
        /// against, so every plant counts as the zone's own.
        /// </summary>
        public void AddPlant(
            string defName,
            bool dying,
            bool blighted,
            bool harvestable,
            bool mature,
            float growth
        )
        {
            if (!string.IsNullOrEmpty(_crop) && defName != _crop)
            {
                ForeignCells++;
                if (!string.IsNullOrEmpty(defName) && !_foreignPlants.Contains(defName))
                {
                    _foreignPlants.Add(defName);
                }
                return;
            }

            SownCells++;
            if (dying)
            {
                DyingCells++;
            }
            if (blighted)
            {
                BlightedCells++;
            }
            if (harvestable)
            {
                HarvestableCells++;
            }
            if (mature)
            {
                MatureCells++;
            }
            if (growth > MaxGrowth)
            {
                MaxGrowth = growth;
            }
        }
    }
}
