using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class MapDto
    {
        [ParameterDescription("The map's numeric id, as the game numbers maps.")]
        public int Id { get; set; }
        [ParameterDescription("The map's position in the game's map list.")]
        public int Index { get; set; }
        [ParameterDescription("The seed the map was generated from.")]
        public int Seed { get; set; }
        [ParameterDescription("The numeric id of the faction that holds the map.")]
        public string FactionId { get; set; }
        [ParameterDescription("Whether this is the player's home map.")]
        public bool IsPlayerHome { get; set; }
        [ParameterDescription("Whether the map is a pocket map, such as a caravan encounter.")]
        public bool IsPocketMap { get; set; }
        [ParameterDescription("Whether an incident created the map for a short while.")]
        public bool IsTempIncidentMap { get; set; }
        [ParameterDescription("How many cells the map spans, as x and z.")]
        public string Size { get; set; }
    }

    public class MapWeatherDto
    {
        [ParameterDescription("The weather the map has right now, as the game names it.")]
        public string Weather { get; set; }
        [ParameterDescription("The outdoor temperature right now, in Celsius.")]
        public float Temperature { get; set; }
    }

    public class MapPowerInfoDto
    {
        [ParameterDescription("How much power the grid makes right now, in watts.")]
        public int CurrentPower { get; set; }
        [ParameterDescription("How much power every producer could make, in watts.")]
        public int TotalPossiblePower { get; set; }
        [ParameterDescription("How much power the batteries hold right now.")]
        public int CurrentlyStoredPower { get; set; }
        [ParameterDescription("How much power the batteries can hold.")]
        public int TotalPowerStorage { get; set; }
        [ParameterDescription("How much power the grid draws, in watts.")]
        public int TotalConsumption { get; set; }
        [ParameterDescription("How much of that draw comes from machines that are switched on.")]
        public int ConsumptionPowerOn { get; set; }
        public List<int> ProducePowerBuildings = new List<int>();
        public List<int> ConsumePowerBuildings = new List<int>();
        public List<int> StorePowerBuildings = new List<int>();
        // Buildings answers every built building with a power comp, one row
        // each (fox ticket #3762). The figures above sum the nets, so a machine
        // no net reaches is in none of them; this row list is where an
        // unconnected stove or cooler becomes visible.
        public List<PoweredBuildingDto> Buildings = new List<PoweredBuildingDto>();

        // TicksSinceLoad is how many ticks the colony has run since it was
        // loaded, -1 when this build saw no load (fox ticket #3781). Every
        // figure above is written by the game's tick, and the tick the loader
        // itself runs still reads 0 W produced of 1700 W installed, so a colony
        // that has run almost nothing since its load is not measured: the same
        // colony read 1700 W one `wait` later.
        [ParameterDescription("TicksSinceLoad is how many ticks the colony has run since it was loaded, -1 when this build saw no load (fox ticket #3781).")]
        public int TicksSinceLoad { get; set; } = -1;
    }

    public class PoweredBuildingDto
    {
        [ParameterDescription("The building's numeric id.")]
        public int Id { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public PositionDto Position { get; set; }

        // Consumes is what the building draws when it runs, Produces what a
        // plant gives right now, Nominal what it gives at full strength.
        [ParameterDescription("Consumes is what the building draws when it runs, Produces what a plant gives right now, Nominal what it gives at full strength.")]
        public float Consumes { get; set; }
        [ParameterDescription("How much power the building makes, in watts.")]
        public float Produces { get; set; }
        [ParameterDescription("How much power it would make at full output.")]
        public float Nominal { get; set; }

        // Connected says a power net reaches the building at all; Powered is
        // the game's own answer to "does it have power now", so a consumer on a
        // net without a producer is connected and still dead.
        [ParameterDescription("Connected says a power net reaches the building at all; Powered is the game's own answer to \"does it have power now\", so a consumer on a net without a producer is connected and still dead.")]
        public bool Connected { get; set; }
        [ParameterDescription("Whether the building has power.")]
        public bool Powered { get; set; }

        // SwitchedOn is the player's own switch: a machine switched off is not
        // a wiring fault and needs a different lever.
        [ParameterDescription("SwitchedOn is the player's own switch: a machine switched off is not a wiring fault and needs a different lever.")]
        public bool SwitchedOn { get; set; }

        // NetProduction and NetStored describe the net the building stands on,
        // in W and Wd, so a connected building on a dead net can say why.
        [ParameterDescription("NetProduction and NetStored describe the net the building stands on, in W and Wd, so a connected building on a dead net can say why.")]
        public float NetProduction { get; set; }
        [ParameterDescription("How much of the grid's stored power this net holds.")]
        public float NetStored { get; set; }

        // ConduitDistance is how many cells away the nearest conduit cell
        // stands, -1 when the map holds none; ConduitPosition names that cell.
        [ParameterDescription("ConduitDistance is how many cells away the nearest conduit cell stands, -1 when the map holds none; ConduitPosition names that cell.")]
        public int ConduitDistance { get; set; }
        [ParameterDescription("The cell of the conduit the building connects through.")]
        public PositionDto ConduitPosition { get; set; }

        // NetId numbers the net the building stands on: its index in the map's
        // own net list plus one, 0 when no net reaches it (fox ticket #3902).
        // Two rows are on the same net when their ids match; the figures alone
        // cannot tell two dead nets apart, so a consumer on one net would name
        // a producer standing on another as its cause.
        [ParameterDescription("NetId numbers the net the building stands on: its index in the map's own net list plus one, 0 when no net reaches it (fox ticket #3902).")]
        public int NetId { get; set; }

        // Plant names the power plant comp when the building is a producer
        // ("CompPowerPlantSolar", "CompPowerPlantWind"), empty for anything
        // else (fox ticket #3902). It is what separates a solar panel that is
        // dark from a generator that ran dry: both give 0 W, and the cure
        // differs.
        [ParameterDescription("Plant names the power plant comp when the building is a producer (\"CompPowerPlantSolar\", \"CompPowerPlantWind\"), empty for anything else (fox ticket #3902).")]
        public string Plant { get; set; }

        // BrokenDown is the game's own "this one needs repair" (fox ticket
        // #3902): a broken generator gives 0 W and reads exactly like one out
        // of fuel until this field says which it is.
        [ParameterDescription("BrokenDown is the game's own \"this one needs repair\" (fox ticket #3902): a broken generator gives 0 W and reads exactly like one out of fuel until this field says which it is.")]
        public bool BrokenDown { get; set; }
    }

    public class MapTimeDto
    {
        [ParameterDescription("The colony's date and time, as the game prints them.")]
        public string Datetime { get; set; }
    }

    public class MapCreaturesSummaryDto
    {
        [ParameterDescription("How many free colonists stand on the map.")]
        public int ColonistsCount { get; set; }
        [ParameterDescription("How many prisoners the colony holds here.")]
        public int PrisonersCount { get; set; }
        [ParameterDescription("How many hostiles stand on the map.")]
        public int EnemiesCount { get; set; }
        // HeldCount is how many of EnemiesCount the colony itself holds: a
        // prisoner or a slave that is not fighting (fox ticket #4187). An
        // enemy faction stays hostile in the game's eyes, so a prisoner in the
        // colony's own cell is in the hostile list; the danger line must not
        // read that as a raid, and only the mod can tell it from a prison
        // break, which keeps its cap through LordJob_PrisonBreak.
        [ParameterDescription("HeldCount is how many of EnemiesCount the colony itself holds: a prisoner or a slave that is not fighting (fox ticket #4187).")]
        public int HeldCount { get; set; }
        [ParameterDescription("How many colony animals stand on the map.")]
        public int AnimalsCount { get; set; }
        [ParameterDescription("How many insectoids stand on the map.")]
        public int InsectoidsCount { get; set; }
        [ParameterDescription("How many mechanoids stand on the map.")]
        public int MechanoidsCount { get; set; }
        // Attackers names the hostiles whose current job acts on a colony pawn
        // (fox ticket #3690): the digest's danger line then says who is being
        // beaten, which is the difference between a raid and a wild man at the
        // barracks door.
        [ParameterDescription("Attackers names the hostiles whose current job acts on a colony pawn (fox ticket #3690): the digest's danger line then says who is being beaten, which is the difference between a raid and a wild...")]
        public List<CreatureAttackerDto> Attackers { get; set; } = new List<CreatureAttackerDto>();
    }

    public class CreatureAttackerDto
    {
        [ParameterDescription("The attacker's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("What kind of pawn attacks, e.g. a raider or a manhunter animal.")]
        public string Kind { get; set; }
        [ParameterDescription("The numeric id of what it attacks.")]
        public int TargetId { get; set; }
        [ParameterDescription("The thing it attacks, as the game names it.")]
        public string Target { get; set; }
    }

    public class MapFarmSummaryDto
    {
        [ParameterDescription("How many growing zones the map holds.")]
        public int TotalGrowingZones { get; set; }
        [ParameterDescription("How many plants stand in those zones.")]
        public int TotalPlants { get; set; }
        [ParameterDescription("How much food the zones will yield when ripe.")]
        public int TotalExpectedYield { get; set; }
        [ParameterDescription("How many of those plants the blight infects.")]
        public int TotalInfectedPlants { get; set; }
        // GrowthProgressAverage is a 0..1 fraction averaged over the plants,
        // the same scale the game's Plant.Growth uses. MaxGrowthProgress is
        // the ripest plant's, so a field reads as ready even when the average
        // still sits low.
        [ParameterDescription("GrowthProgressAverage is a 0..1 fraction averaged over the plants, the same scale the game's Plant.Growth uses.")]
        public float GrowthProgressAverage { get; set; }
        [ParameterDescription("The growth of the ripest plant, 0 to 1.")]
        public float MaxGrowthProgress { get; set; }
        [ParameterDescription("How many plants are ready to harvest now.")]
        public int TotalHarvestablePlants { get; set; }
        // TotalMaturePlants counts the plants the growing zone's harvest takes
        // (fox ticket #3677): TotalHarvestablePlants is HarvestableNow, which
        // starts below full growth, so a field of 34 ripe rice read the same
        // once three of them matured and the ripe count stopped being the
        // answer to "how much can be picked right now".
        [ParameterDescription("TotalMaturePlants counts the plants the growing zone's harvest takes (fox ticket #3677): TotalHarvestablePlants is HarvestableNow, which starts below full growth, so a field of 34 ripe rice read...")]
        public int TotalMaturePlants { get; set; }
        [ParameterDescription("How many days until the next plant ripens.")]
        public float DaysUntilNextHarvest { get; set; }
        [ParameterDescription("One entry per crop the zones grow.")]
        public List<CropTypeDto> CropTypes { get; set; } = new List<CropTypeDto>();
    }

    public class CropTypeDto
    {
        [ParameterDescription("Which crop the zone sows, named by its def.")]
        public string PlantDefName { get; set; }
        [ParameterDescription("The crop's label, as the game shows it.")]
        public string PlantLabel { get; set; }
        [ParameterDescription("What the crop is for, e.g. food or drug.")]
        public string PlantCategory { get; set; }
        [ParameterDescription("How many plants of this crop stand in the zones.")]
        public int TotalPlants { get; set; }
        [ParameterDescription("How many of them are ready to pick.")]
        public int HarvestablePlants { get; set; }
        [ParameterDescription("How much food the crop will yield.")]
        public int ExpectedYield { get; set; }
        [ParameterDescription("How many of them the blight infects.")]
        public int InfectedCount { get; set; }
        [ParameterDescription("The average growth of the crop, 0 to 1.")]
        public float GrowthProgressAverage { get; set; }
        [ParameterDescription("The growth of the ripest plant, 0 to 1.")]
        public float MaxGrowthProgress { get; set; }
        [ParameterDescription("How many days until the next plant of this crop ripens.")]
        public float DaysUntilHarvest { get; set; }
        [ParameterDescription("Whether every plant of the crop is fully grown.")]
        public bool IsFullyGrown { get; set; }
        [ParameterDescription("Whether any plant of the crop can be picked now.")]
        public bool IsHarvestable { get; set; }
        public int ZoneId { get; set; }
    }

    public class GrowingZoneDto
    {
        [ParameterDescription("The zone's numeric id.")]
        public ZoneDto Zone { get; set; }
        [ParameterDescription("Which crop the zone grows, named by its def.")]
        public string PlantDefName { get; set; }
        [ParameterDescription("How many plants stand in the zone.")]
        public int PlantCount { get; set; }
        [ParameterDescription("What one plant of the crop yields.")]
        public int DefExpectedYield { get; set; }
        [ParameterDescription("What the zone's plants yield in total.")]
        public int ExpectedYield { get; set; }
        [ParameterDescription("How many plants the blight infects.")]
        public int InfectedCount { get; set; }
        [ParameterDescription("The zone's average growth, 0 to 1.")]
        public float GrowthProgress { get; set; }
        [ParameterDescription("Whether the zone has sowing switched on.")]
        public bool IsSowing { get; set; }

        // The Cut gizmo (fox ticket #4316): a growing zone with cut off leaves
        // the trees standing inside it, and the zone read said nothing about it.
        [ParameterDescription("The Cut gizmo (fox ticket #4316): a growing zone with cut off leaves the trees standing inside it, and the zone read said nothing about it.")]
        public bool AllowCut { get; set; }

        [ParameterDescription("The ground the zone stands on.")]
        public string SoilType { get; set; }
        [ParameterDescription("How fertile that ground is, 0 to 1.")]
        public float Fertility { get; set; }
        [ParameterDescription("Whether any plant in the zone is dying.")]
        public bool HasDying { get; set; }
        [ParameterDescription("Whether pollution kills plants in the zone.")]
        public bool HasDyingFromPollution { get; set; }
        [ParameterDescription("Whether the absence of pollution kills them.")]
        public bool HasDyingFromNoPollution { get; set; }

        // What a grow call did with the rectangle (fox ticket #3388): a
        // rectangle over an existing growing zone joins it instead of making a
        // stray one-cell zone beside it, so the answer has to say which of the
        // two happened and how many cells it left alone.
        // Mode is "created" or "extended".
        [ParameterDescription("What a grow call did with the rectangle (fox ticket #3388): a rectangle over an existing growing zone joins it instead of making a stray one-cell zone beside it, so the answer has to say which of...")]
        public string Mode { get; set; }
        [ParameterDescription("Whether the call changed the zone's crop.")]
        public bool PlantChanged { get; set; }
        [ParameterDescription("How many cells the call added to the zone.")]
        public int CellsAdded { get; set; }
        [ParameterDescription("How many cells already belonged to this zone.")]
        public int CellsInTarget { get; set; }
        [ParameterDescription("How many cells belong to another zone.")]
        public int CellsInOtherZone { get; set; }
        [ParameterDescription("How many cells lie outside the map.")]
        public int CellsOutOfBounds { get; set; }
    }

    public class MapZonesDto
    {
        [ParameterDescription("Every zone on the map.")]
        public List<ZoneDto> Zones { get; set; }
        [ParameterDescription("Every named area on the map.")]
        public List<ZoneDto> Areas { get; set; }
    }

    public class ZoneDto
    {
        [ParameterDescription("The zone's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("How many cells the zone holds.")]
        public int CellsCount { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The zone's name without the game's numbering.")]
        public string BaseLabel { get; set; }
        [ParameterDescription("The zone's kind, e.g. stockpile or growing.")]
        public string Type { get; set; }

        // Null for anything that is not a stockpile: a growing zone or an area
        // has no storage filter.
        [ParameterDescription("Null for anything that is not a stockpile: a growing zone or an area has no storage filter.")]
        public StockpileFilterDto Storage { get; set; }

        // Growing is what a growing zone is set to grow and how much of it is
        // actually sown; null for anything else (fox ticket #3277). A rice field
        // that produced nothing for four game hours read as "339 cells" and no
        // other word, so no caller could tell a zone without a plant from one
        // whose cells still hold dead plants.
        [ParameterDescription("Growing is what a growing zone is set to grow and how much of it is actually sown; null for anything else (fox ticket #3277).")]
        public ZoneGrowingDto Growing { get; set; }
    }

    // ZoneGrowingDto is one growing zone's plant and its cell counts. Unsown
    // cells are the cells without a plant: no soil, out of season, or a dead
    // plant still standing there. ForeignCells are cells holding a plant that
    // is not the zone's crop (fox ticket #3805), and ForeignPlants names them,
    // so the three counts add up to the zone's cells.
    public class ZoneGrowingDto
    {
        [ParameterDescription("Which crop the zone grows, named by its def.")]
        public string PlantDefName { get; set; }
        [ParameterDescription("The crop's label, as the game shows it.")]
        public string PlantLabel { get; set; }
        [ParameterDescription("Whether the zone's Sow button is on.")]
        public bool AllowSow { get; set; }

        // AllowCut is the zone's second gizmo toggle (fox ticket #4316): a
        // growing zone whose cut is off leaves the trees inside it alone, and
        // nothing in the read said so before.
        [ParameterDescription("AllowCut is the zone's second gizmo toggle (fox ticket #4316): a growing zone whose cut is off leaves the trees inside it alone, and nothing in the read said so before.")]
        public bool AllowCut { get; set; }
        [ParameterDescription("Whether the crop is in season now.")]
        public bool InSeason { get; set; }
        [ParameterDescription("How many cells carry the crop.")]
        public int SownCells { get; set; }
        [ParameterDescription("How many cells stand empty.")]
        public int UnsownCells { get; set; }
        [ParameterDescription("How many cells grow another plant.")]
        public int ForeignCells { get; set; }
        [ParameterDescription("The other plants the zone's cells grow.")]
        public List<string> ForeignPlants { get; set; }
        [ParameterDescription("How many cells carry a dying plant.")]
        public int DyingCells { get; set; }
        [ParameterDescription("How many cells the blight infects.")]
        public int BlightedCells { get; set; }
        [ParameterDescription("How many cells are ready to pick.")]
        public int HarvestableCells { get; set; }
        // MatureCells and MaxGrowth answer the question HarvestableCells
        // cannot: whether the game will harvest this field now (fox ticket
        // #3396). A growing zone is harvested only at PlantLifeStage.Mature,
        // which starts above 0.999 growth, while HarvestableNow already counts
        // a plant from its harvestMinGrowth (0.65 for rice).
        [ParameterDescription("MatureCells and MaxGrowth answer the question HarvestableCells cannot: whether the game will harvest this field now (fox ticket #3396).")]
        public int MatureCells { get; set; }
        [ParameterDescription("The growth of the ripest plant, 0 to 1.")]
        public float MaxGrowth { get; set; }
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
        [ParameterDescription("How far the plant has grown, 0 to 1.")]
        public float Growth { get; set; }
        [ParameterDescription("Whether the plant can be picked now.")]
        public bool Harvestable { get; set; }
        [ParameterDescription("Whether the plant is dying.")]
        public bool Dying { get; set; }
        [ParameterDescription("Whether the blight infects the plant.")]
        public bool Blighted { get; set; }
    }

    // StockpileFilterDto is one stockpile's filter in the terms the storage tab
    // shows: the priority, whether the filter takes food at all, and whether it
    // takes raw food or meals (fox ticket #3181). A colony starved with 267 rice
    // on the field because no stockpile took RawRice, and no read said so.
    public class StockpileFilterDto
    {
        [ParameterDescription("The stockpile's priority, 1 low to 5 critical.")]
        public string Priority { get; set; }
        [ParameterDescription("Whether the stockpile takes food at all.")]
        public bool AcceptsFood { get; set; }
        [ParameterDescription("Whether it takes raw ingredients.")]
        public bool AcceptsRawFood { get; set; }
        [ParameterDescription("Whether it takes cooked meals.")]
        public bool AcceptsMeals { get; set; }
        [ParameterDescription("How many item defs the filter allows.")]
        public int AllowedDefCount { get; set; }

        // AllowedFoodCategories answers the storage tab's food checkboxes
        // instead of its food-type words (fox ticket #3231): the five categories
        // the tool names (Foods, FoodMeals, FoodRaw, PlantFoodRaw, MeatRaw), in
        // that order, whichever of them the filter still allows a def of.
        [ParameterDescription("AllowedFoodCategories answers the storage tab's food checkboxes instead of its food-type words (fox ticket #3231): the five categories the tool names (Foods, FoodMeals, FoodRaw, PlantFoodRaw,...")]
        public List<string> AllowedFoodCategories { get; set; }

        // AllowedMealDefs names the meal-flagged defs the filter allows. Pemmican
        // and BabyFood carry the game's Meal food type but sit in Foods rather
        // than FoodMeals, so a player who unticks "meals" still stores meals and
        // the food-type answer reads as the opposite of the click.
        [ParameterDescription("AllowedMealDefs names the meal-flagged defs the filter allows.")]
        public List<string> AllowedMealDefs { get; set; }

        // AllowedRawDefs names the raw-food defs the filter allows, the same
        // question as AllowedMealDefs for the other food type. Chocolate,
        // HemogenPack and Kibble carry Raw while sitting in Foods rather than
        // FoodRaw, so a player who unticks "raw food" still stores raw food and
        // the food-type answer reads as the opposite of the click (fox ticket
        // #3303).
        [ParameterDescription("AllowedRawDefs names the raw-food defs the filter allows, the same question as AllowedMealDefs for the other food type.")]
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
        [ParameterDescription("The food stacks the colony cannot store.")]
        public List<ThingDto> Things { get; set; }
        [ParameterDescription("The stacks that found no storage with room.")]
        public List<ThingDto> NoRoomThings { get; set; }
        [ParameterDescription("How many storages the check looked at.")]
        public int StorageCount { get; set; }
        [ParameterDescription("Whether the check counted free space at all.")]
        public bool CapacityChecked { get; set; }
    }

    public class RoomDto
    {
        [ParameterDescription("The room's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("The room's role, as the game names it.")]
        public string RoleLabel { get; set; }
        [ParameterDescription("The room's temperature, in Celsius.")]
        public float Temperature { get; set; }
        [ParameterDescription("How many cells the room holds.")]
        public int CellsCount { get; set; }
        [ParameterDescription("Whether the room reaches the map's edge.")]
        public bool TouchesMapEdge { get; set; }
        [ParameterDescription("Whether the room counts as a prison cell.")]
        public bool IsPrisonCell { get; set; }
        [ParameterDescription("Whether the room is a doorway.")]
        public bool IsDoorway { get; set; }
        [ParameterDescription("How many of its cells lack a roof.")]
        public int OpenRoofCount { get; set; }
        [ParameterDescription("The numeric ids of the beds in the room.")]
        public List<int> ContainedBedsIds { get; set; }

        // ContainedPawnsIds is the humanlike pawns standing in the room (fox
        // ticket #3225). The room read carries no cell list, so without it a
        // caller cannot tell a hot empty workshop from the room a colonist
        // stands in; a bed was the only occupancy signal. Animals are left out
        // on purpose: a pen full of chickens is not a colonist's room.
        [ParameterDescription("ContainedPawnsIds is the humanlike pawns standing in the room (fox ticket #3225).")]
        public List<int> ContainedPawnsIds { get; set; }
    }

    public class MapRoomsDto
    {
        [ParameterDescription("Every room on the map.")]
        public List<RoomDto> Rooms { get; set; }
    }

    public class DestroyRectRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("One corner of the rectangle to destroy.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the rectangle.")]
        public PositionDto PointB { get; set; }
    }

    public class RepairPositionsRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("The cells to repair, as x and z pairs.")]
        public List<PositionDto> Positions { get; set; }
    }

    public class RepairRectRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("One corner of the rectangle to repair.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the rectangle.")]
        public PositionDto PointB { get; set; }
    }

    public class CreateGrowingZoneRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("The crop to sow, by def name.")]
        public string PlantDef { get; set; }
        [ParameterDescription("One corner of the zone's rectangle.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the rectangle.")]
        public PositionDto PointB { get; set; }
    }

    public class FogGridDto
    {
        public int MapId { get; set; }
        [ParameterDescription("How many cells wide the fog grid is.")]
        public int Width { get; set; }
        [ParameterDescription("How many cells tall the fog grid is.")]
        public int Height { get; set; }
        [ParameterDescription("One flag per cell, row by row: true where the fog still hides the cell.")]
        public string FogData { get; set; }
    }

    public class CreateStockpileRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("One corner of the stockpile's rectangle.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the rectangle.")]
        public PositionDto PointB { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The stockpile's priority, 1 low to 5 critical.")]
        public int? Priority { get; set; }
        [ParameterDescription("The item defs the filter allows.")]
        public List<string> AllowedItemDefs { get; set; }
        [ParameterDescription("The item categories the filter allows.")]
        public List<string> AllowedItemCategories { get; set; }
        [ParameterDescription("The lowest hit point percentage the stockpile takes.")]
        public float? MinHitPointsPercent { get; set; }
        [ParameterDescription("The highest hit point percentage it takes.")]
        public float? MaxHitPointsPercent { get; set; }
        [ParameterDescription("The lowest quality it takes.")]
        public string MinQuality { get; set; }
        [ParameterDescription("The highest quality it takes.")]
        public string MaxQuality { get; set; }
    }

    public class StockpileResponseDto
    {
        public int ZoneId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("How many cells the stockpile holds.")]
        public int CellsCount { get; set; }
        [ParameterDescription("The stockpile's priority, 1 low to 5 critical.")]
        public int Priority { get; set; }
        [ParameterDescription("Whether the call made the zone.")]
        public bool Success { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Message { get; set; }

        // A filter write answers what it applied and what it could not resolve
        // (fox ticket #3206): AppliedDefs and AppliedCategories name the names
        // that changed the filter, Unknown the ones neither the thing defs nor
        // the thing categories know, and Filter is the stockpile's filter after
        // the write, in the same terms the zones read prints.
        [ParameterDescription("A filter write answers what it applied and what it could not resolve (fox ticket #3206): AppliedDefs and AppliedCategories name the names that changed the filter, Unknown the ones neither the...")]
        public List<string> AppliedDefs { get; set; }
        [ParameterDescription("The categories the filter now allows.")]
        public List<string> AppliedCategories { get; set; }
        [ParameterDescription("The names the call did not recognise.")]
        public List<string> Unknown { get; set; }
        [ParameterDescription("The filter as the stockpile holds it.")]
        public StockpileFilterDto Filter { get; set; }
    }

    public class UpdateStockpileRequestDto
    {
        public int ZoneId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The stockpile's priority, 1 low to 5 critical.")]
        public int? Priority { get; set; }
        [ParameterDescription("The item defs to allow.")]
        public List<string> AddItemDefs { get; set; }
        [ParameterDescription("The item defs to stop allowing.")]
        public List<string> RemoveItemDefs { get; set; }
        [ParameterDescription("The item categories to allow in the filter.")]
        public List<string> AddItemCategories { get; set; }
        [ParameterDescription("The categories to stop allowing.")]
        public List<string> RemoveItemCategories { get; set; }
        [ParameterDescription("The lowest hit point percentage the stockpile takes.")]
        public float? MinHitPointsPercent { get; set; }
        [ParameterDescription("The highest hit point percentage it takes.")]
        public float? MaxHitPointsPercent { get; set; }
        [ParameterDescription("The lowest quality it takes.")]
        public string MinQuality { get; set; }
        [ParameterDescription("The highest quality it takes.")]
        public string MaxQuality { get; set; }
    }
}
