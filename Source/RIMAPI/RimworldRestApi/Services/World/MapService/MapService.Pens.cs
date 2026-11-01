using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// The animal pens with the game's own enclosure and food numbers
// (split out of MapService.cs, fox #3659).

namespace RIMAPI.Services
{
    public partial class MapService
    {
        /// <summary>
        /// Every animal pen on the map: the marker's cell, the game's own
        /// enclosure verdict, the animals standing inside and the food numbers
        /// the marker's pen tab shows (fox ticket #3239). Everything here comes
        /// from the game's calculator, so a caller never re-derives a capacity
        /// from the cell count.
        /// </summary>
        public ApiResult<List<PenDto>> GetMapPens(int mapId)
        {
            var map = MapHelper.GetMapByID(mapId);
            if (map == null)
            {
                return ApiResult<List<PenDto>>.Fail($"Map not found: {mapId}");
            }

            var result = new List<PenDto>();
            var markerDef = DefDatabase<ThingDef>.GetNamedSilentFail("PenMarker");
            if (markerDef == null)
            {
                return ApiResult<List<PenDto>>.Ok(result);
            }

            foreach (var thing in map.listerThings.ThingsOfDef(markerDef))
            {
                var comp = (thing as ThingWithComps)?.GetComp<CompAnimalPenMarker>();
                if (comp == null)
                {
                    continue;
                }

                var state = comp.PenState;
                // The calculator is the tab's own data: cells, growth, stock and
                // the animals it counted while walking the pen's cells.
                var food = comp.PenFoodCalculator;
                var quadrum = food.GetSummerOrBestQuadrum();

                var dto = new PenDto
                {
                    MarkerId = thing.thingIDNumber,
                    Label = comp.InspectLabel,
                    DefName = thing.def.defName,
                    X = thing.Position.x,
                    Z = thing.Position.z,
                    Enclosed = state.Enclosed,
                    Unenclosed = state.Unenclosed,
                    HasOutsideAccess = state.HasOutsideAccess,
                    PassableDoors = state.PassableDoors,
                    Cells = food.numCells,
                    SoilCells = food.numCellsSoil,
                    NutritionPerDay = food.NutritionPerDayToday,
                    ConsumptionPerDay = food.SumNutritionConsumptionPerDay,
                    StockpiledNutrition = food.sumStockpiledNutritionAvailableNow,
                    Animals = new List<PenAnimalDto>(),
                    Members = new List<PenAnimalMemberDto>(),
                    Stockpiled = new List<PenFoodDto>(),
                };

                foreach (var info in food.ActualAnimalInfos)
                {
                    if (info.animalDef == null)
                    {
                        continue;
                    }
                    dto.Animals.Add(new PenAnimalDto
                    {
                        DefName = info.animalDef.defName,
                        Label = info.animalDef.label,
                        Count = info.TotalCount,
                        NutritionPerDay = info.TotalNutritionConsumptionPerDay,
                        Capacity = food.CapacityOf(quadrum, info.animalDef),
                    });
                }

                foreach (var item in food.AllStockpiledInfos)
                {
                    if (item.itemDef == null)
                    {
                        continue;
                    }
                    dto.Stockpiled.Add(new PenFoodDto
                    {
                        DefName = item.itemDef.defName,
                        Label = item.itemDef.label,
                        Count = item.totalCount,
                        Nutrition = item.totalNutritionAvailable,
                    });
                }

                // The calculator counts animals cell by cell; a caller that wants
                // to name one needs the pawns themselves, so the pen's connected
                // regions decide which colony animal stands inside.
                foreach (var pawn in map.mapPawns.SpawnedColonyAnimals)
                {
                    if (pawn == null || !comp.AcceptsToPen(pawn))
                    {
                        continue;
                    }
                    var region = map.regionGrid.GetValidRegionAt(pawn.Position);
                    if (region == null || !state.ContainsConnectedRegion(region))
                    {
                        continue;
                    }
                    dto.Members.Add(new PenAnimalMemberDto
                    {
                        Id = pawn.thingIDNumber,
                        Name = pawn.LabelShort,
                        DefName = pawn.def.defName,
                    });
                }

                result.Add(dto);
            }

            return ApiResult<List<PenDto>>.Ok(result);
        }
    }
}
