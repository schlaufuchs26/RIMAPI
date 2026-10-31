using System.Collections.Generic;

namespace RIMAPI.Models
{
    public class MapDto
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public int Seed { get; set; }
        public string FactionId { get; set; }
        public bool IsPlayerHome { get; set; }
        public bool IsPocketMap { get; set; }
        public bool IsTempIncidentMap { get; set; }
        public string Size { get; set; }
    }

    public class MapWeatherDto
    {
        public string Weather { get; set; }
        public float Temperature { get; set; }
    }

    public class MapPowerInfoDto
    {
        public int CurrentPower { get; set; }
        public int TotalPossiblePower { get; set; }
        public int CurrentlyStoredPower { get; set; }
        public int TotalPowerStorage { get; set; }
        public int TotalConsumption { get; set; }
        public int ConsumptionPowerOn { get; set; }
        public List<int> ProducePowerBuildings = new List<int>();
        public List<int> ConsumePowerBuildings = new List<int>();
        public List<int> StorePowerBuildings = new List<int>();
    }

    public class MapTimeDto
    {
        public string Datetime { get; set; }
    }

    public class AnimalDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Def { get; set; }
        public string Faction { get; set; }
        public PositionDto Position { get; set; }
        public int? Trainer { get; set; }
        public bool Pregnant { get; set; }
    }

    public class MapCreaturesSummaryDto
    {
        public int ColonistsCount { get; set; }
        public int PrisonersCount { get; set; }
        public int EnemiesCount { get; set; }
        public int AnimalsCount { get; set; }
        public int InsectoidsCount { get; set; }
        public int MechanoidsCount { get; set; }
    }

    public class MapFarmSummaryDto
    {
        public int TotalGrowingZones { get; set; }
        public int TotalPlants { get; set; }
        public int TotalExpectedYield { get; set; }
        public int TotalInfectedPlants { get; set; }
        // GrowthProgressAverage is a 0..1 fraction averaged over the plants,
        // the same scale the game's Plant.Growth uses. MaxGrowthProgress is
        // the ripest plant's, so a field reads as ready even when the average
        // still sits low.
        public float GrowthProgressAverage { get; set; }
        public float MaxGrowthProgress { get; set; }
        public int TotalHarvestablePlants { get; set; }
        public float DaysUntilNextHarvest { get; set; }
        public List<CropTypeDto> CropTypes { get; set; } = new List<CropTypeDto>();
    }

    public class CropTypeDto
    {
        public string PlantDefName { get; set; }
        public string PlantLabel { get; set; }
        public string PlantCategory { get; set; }
        public int TotalPlants { get; set; }
        public int HarvestablePlants { get; set; }
        public int ExpectedYield { get; set; }
        public int InfectedCount { get; set; }
        public float GrowthProgressAverage { get; set; }
        public float MaxGrowthProgress { get; set; }
        public float DaysUntilHarvest { get; set; }
        public bool IsFullyGrown { get; set; }
        public bool IsHarvestable { get; set; }
        public int ZoneId { get; set; }
    }

    public class GrowingZoneDto
    {
        public ZoneDto Zone { get; set; }
        public string PlantDefName { get; set; }
        public int PlantCount { get; set; }
        public int DefExpectedYield { get; set; }
        public int ExpectedYield { get; set; }
        public int InfectedCount { get; set; }
        public float GrowthProgress { get; set; }
        public bool IsSowing { get; set; }
        public string SoilType { get; set; }
        public float Fertility { get; set; }
        public bool HasDying { get; set; }
        public bool HasDyingFromPollution { get; set; }
        public bool HasDyingFromNoPollution { get; set; }
    }

    public class MapZonesDto
    {
        public List<ZoneDto> Zones { get; set; }
        public List<ZoneDto> Areas { get; set; }
    }

    public class ZoneDto
    {
        public int Id { get; set; }
        public int CellsCount { get; set; }
        public string Label { get; set; }
        public string BaseLabel { get; set; }
        public string Type { get; set; }

        // Null for anything that is not a stockpile: a growing zone or an area
        // has no storage filter.
        public StockpileFilterDto Storage { get; set; }

        // Growing is what a growing zone is set to grow and how much of it is
        // actually sown; null for anything else (fox ticket #3277). A rice field
        // that produced nothing for four game hours read as "339 cells" and no
        // other word, so no caller could tell a zone without a plant from one
        // whose cells still hold dead plants.
        public ZoneGrowingDto Growing { get; set; }
    }

    // ZoneGrowingDto is one growing zone's plant and its cell counts. Unsown
    // cells are the cells without a plant: no soil, out of season, or a dead
    // plant still standing there.
    public class ZoneGrowingDto
    {
        public string PlantDefName { get; set; }
        public string PlantLabel { get; set; }
        public bool AllowSow { get; set; }
        public bool InSeason { get; set; }
        public int SownCells { get; set; }
        public int UnsownCells { get; set; }
        public int DyingCells { get; set; }
        public int BlightedCells { get; set; }
        public int HarvestableCells { get; set; }
    }

    // PlantDto is one plant on a rectangle (fox ticket #3277): the read a field
    // is diagnosed with, because `map/plants` answers every plant on the map
    // (6.5 MB) and `things` lists no plants at all.
    public class PlantDto
    {
        public int ThingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public float Growth { get; set; }
        public bool Harvestable { get; set; }
        public bool Dying { get; set; }
        public bool Blighted { get; set; }
    }

    // StockpileFilterDto is one stockpile's filter in the terms the storage tab
    // shows: the priority, whether the filter takes food at all, and whether it
    // takes raw food or meals (fox ticket #3181). A colony starved with 267 rice
    // on the field because no stockpile took RawRice, and no read said so.
    public class StockpileFilterDto
    {
        public string Priority { get; set; }
        public bool AcceptsFood { get; set; }
        public bool AcceptsRawFood { get; set; }
        public bool AcceptsMeals { get; set; }
        public int AllowedDefCount { get; set; }

        // AllowedFoodCategories answers the storage tab's food checkboxes
        // instead of its food-type words (fox ticket #3231): the five categories
        // the tool names (Foods, FoodMeals, FoodRaw, PlantFoodRaw, MeatRaw), in
        // that order, whichever of them the filter still allows a def of.
        public List<string> AllowedFoodCategories { get; set; }

        // AllowedMealDefs names the meal-flagged defs the filter allows. Pemmican
        // and BabyFood carry the game's Meal food type but sit in Foods rather
        // than FoodMeals, so a player who unticks "meals" still stores meals and
        // the food-type answer reads as the opposite of the click.
        public List<string> AllowedMealDefs { get; set; }

        // AllowedRawDefs names the raw-food defs the filter allows, the same
        // question as AllowedMealDefs for the other food type. Chocolate,
        // HemogenPack and Kibble carry Raw while sitting in Foods rather than
        // FoodRaw, so a player who unticks "raw food" still stores raw food and
        // the food-type answer reads as the opposite of the click (fox ticket
        // #3303).
        public List<string> AllowedRawDefs { get; set; }
    }

    // UnwantedFoodDto is the answer to "which edible things lie on the ground
    // with nowhere to go": the loose food no stockpile or storage building
    // accepts, the loose food a filter takes but no cell can hold (a full
    // shelf), and how many storages were asked (fox tickets #3181, #3207).
    // CapacityChecked says whether the second question could be asked at all:
    // it needs a colonist who can haul, and no hauler means no answer.
    public class UnwantedFoodDto
    {
        public List<ThingDto> Things { get; set; }
        public List<ThingDto> NoRoomThings { get; set; }
        public int StorageCount { get; set; }
        public bool CapacityChecked { get; set; }
    }

    public class RoomDto
    {
        public int Id { get; set; }
        public string RoleLabel { get; set; }
        public float Temperature { get; set; }
        public int CellsCount { get; set; }
        public bool TouchesMapEdge { get; set; }
        public bool IsPrisonCell { get; set; }
        public bool IsDoorway { get; set; }
        public int OpenRoofCount { get; set; }
        public List<int> ContainedBedsIds { get; set; }

        // ContainedPawnsIds is the humanlike pawns standing in the room (fox
        // ticket #3225). The room read carries no cell list, so without it a
        // caller cannot tell a hot empty workshop from the room a colonist
        // stands in; a bed was the only occupancy signal. Animals are left out
        // on purpose: a pen full of chickens is not a colonist's room.
        public List<int> ContainedPawnsIds { get; set; }
    }

    public class MapRoomsDto
    {
        public List<RoomDto> Rooms { get; set; }
    }

    public class DestroyRectRequestDto
    {
        public int MapId { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }

    public class RepairPositionsRequestDto
    {
        public int MapId { get; set; }
        public List<PositionDto> Positions { get; set; }
    }

    public class RepairRectRequestDto
    {
        public int MapId { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }

    public class CreateGrowingZoneRequestDto
    {
        public int MapId { get; set; }
        public string PlantDef { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }

    public class FogGridDto
    {
        public int MapId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string FogData { get; set; }
    }

    public class CreateStockpileRequestDto
    {
        public int MapId { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
        public string Name { get; set; }
        public int? Priority { get; set; }
        public List<string> AllowedItemDefs { get; set; }
        public List<string> AllowedItemCategories { get; set; }
        public float? MinHitPointsPercent { get; set; }
        public float? MaxHitPointsPercent { get; set; }
        public string MinQuality { get; set; }
        public string MaxQuality { get; set; }
    }

    public class StockpileResponseDto
    {
        public int ZoneId { get; set; }
        public string Name { get; set; }
        public int CellsCount { get; set; }
        public int Priority { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        // A filter write answers what it applied and what it could not resolve
        // (fox ticket #3206): AppliedDefs and AppliedCategories name the names
        // that changed the filter, Unknown the ones neither the thing defs nor
        // the thing categories know, and Filter is the stockpile's filter after
        // the write, in the same terms the zones read prints.
        public List<string> AppliedDefs { get; set; }
        public List<string> AppliedCategories { get; set; }
        public List<string> Unknown { get; set; }
        public StockpileFilterDto Filter { get; set; }
    }

    public class UpdateStockpileRequestDto
    {
        public int ZoneId { get; set; }
        public string Name { get; set; }
        public int? Priority { get; set; }
        public List<string> AddItemDefs { get; set; }
        public List<string> RemoveItemDefs { get; set; }
        public List<string> AddItemCategories { get; set; }
        public List<string> RemoveItemCategories { get; set; }
        public float? MinHitPointsPercent { get; set; }
        public float? MaxHitPointsPercent { get; set; }
        public string MinQuality { get; set; }
        public string MaxQuality { get; set; }
    }
}
