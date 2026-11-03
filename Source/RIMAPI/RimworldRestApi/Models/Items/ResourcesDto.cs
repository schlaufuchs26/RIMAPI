using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ResourcesSummaryDto
    {
        [ParameterDescription("How many item stacks the colony holds.")]
        public int TotalItems { get; set; }
        [ParameterDescription("What all of it is worth in silver.")]
        public double TotalMarketValue { get; set; }
        [ParameterDescription("The tick the summary was built on.")]
        public string LastUpdated { get; set; }
        [ParameterDescription("One entry per resource category.")]
        public List<ResourceCategoryDto> Categories { get; set; }
        [ParameterDescription("The food, medicine and weapons the colony must not run out of.")]
        public CriticalResourcesDto CriticalResources { get; set; }
    }

    public class StoragesSummaryDto
    {
        [ParameterDescription("How many stockpiles the map holds.")]
        public int TotalStockpiles { get; set; }
        [ParameterDescription("How many cells they cover.")]
        public int TotalCells { get; set; }
        [ParameterDescription("How many of those cells hold something.")]
        public int UsedCells { get; set; }
        [ParameterDescription("How full the storages are, in percent.")]
        public int UtilizationPercent { get; set; }
    }

    public class ResourceCategoryDto
    {
        [ParameterDescription("The name of the resource category.")]
        public string Category { get; set; }
        public int Count { get; set; }
        [ParameterDescription("What the category is worth in silver.")]
        public double MarketValue { get; set; }
    }

    public class CriticalResourcesDto
    {
        [ParameterDescription("The colony's food numbers.")]
        public ResourcesFoodSummaryDto FoodSummary { get; set; }
        [ParameterDescription("How many medicine units the colony holds.")]
        public int MedicineTotal { get; set; }
        [ParameterDescription("How many weapons the colony holds.")]
        public int WeaponCount { get; set; }
        [ParameterDescription("What those weapons are worth.")]
        public double WeaponValue { get; set; }

        // FuelSummary is the wood and the tanks (fox ticket #3678). A colony
        // ran its stove dry without a single read saying so: the stock line
        // listed food, medicine and weapons, and cooking simply stopped.
        [ParameterDescription("FuelSummary is the wood and the tanks (fox ticket #3678).")]
        public FuelSummaryDto FuelSummary { get; set; }
    }

    // FuelSummaryDto is the colony's fuel: what lies loose on the map, and
    // every building whose tank holds it. Wood is both the fuel and the
    // cheapest building material, so it is named even at zero.
    public class FuelSummaryDto
    {
        [ParameterDescription("The fuel stacks the colony holds.")]
        public List<FuelStockDto> Stocks { get; set; } = new List<FuelStockDto>();
        [ParameterDescription("The buildings that burn fuel.")]
        public List<FuelBuildingDto> Buildings { get; set; } = new List<FuelBuildingDto>();
    }

    public class FuelStockDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
    }

    // FuelBuildingDto is one building with a fuel tank (a fueled stove, a
    // fueled generator, a torch). HasFuel is the component's own verdict, so a
    // caller never has to compare Fuel against zero itself; MealBills tells a
    // stove from a lamp, because only a stove's bills make meals.
    //
    // RefuelAvailable is the game's own answer to "would a colonist refill this
    // tank now" (WorkGiver_Refuel's check, fox ticket #3731): an empty tank
    // nobody refuels is the one worth a line. RefuelForced is the wider gate:
    // a colonist would refill it if told to, so only the building's own
    // auto-refuel is off. RefuelBlocked carries the game's refusal when neither
    // holds ("No fuel to refuel (wood)"). All three are only filled for an
    // empty tank.
    public class FuelBuildingDto
    {
        [ParameterDescription("The building's numeric id.")]
        public int Id { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How much fuel it holds.")]
        public float Fuel { get; set; }
        [ParameterDescription("How much fuel it can hold.")]
        public float FuelCapacity { get; set; }
        [ParameterDescription("The fuel's def name.")]
        public string FuelType { get; set; }
        [ParameterDescription("Whether it has any fuel.")]
        public bool HasFuel { get; set; }
        [ParameterDescription("Whether a colonist may refuel it.")]
        public bool RefuelAvailable { get; set; }
        [ParameterDescription("Whether refuelling is forced.")]
        public bool RefuelForced { get; set; }
        [ParameterDescription("Why refuelling is blocked, when it is.")]
        public string RefuelBlocked { get; set; }
        [ParameterDescription("The bills the building runs right now.")]
        public int Bills { get; set; }
        [ParameterDescription("The bills the building runs that cook meals.")]
        public int MealBills { get; set; }
    }

    public class ResourcesFoodSummaryDto
    {
        [ParameterDescription("How much food the colony holds.")]
        public int FoodTotal { get; set; }
        [ParameterDescription("How much nutrition all of it holds.")]
        public float TotalNutrition { get; set; }
        [ParameterDescription("How many cooked meals it holds.")]
        public int MealsCount { get; set; }
        [ParameterDescription("How many raw ingredients it holds.")]
        public int RawFoodCount { get; set; }
        [ParameterDescription("How much of the food is about to rot.")]
        public RotStatusInfoDto RotStatusInfo { get; set; }
    }

    public class RotStatusInfoDto
    {
        [ParameterDescription("How much nutrition will rot soon.")]
        public float NutritionRotatingSoon { get; set; }
        [ParameterDescription("How much nutrition keeps.")]
        public float NutritionNotRotating { get; set; }
        [ParameterDescription("How much of the food will rot soon, in percent.")]
        public float PercentageRotatingSoon { get; set; }
        [ParameterDescription("The stacks that will rot soon.")]
        public List<RottingFoodItemDto> SoonRottingItems { get; set; } =
            new List<RottingFoodItemDto>();
        [ParameterDescription("How many units will rot soon.")]
        public int TotalSoonRottingItems { get; set; }
        [ParameterDescription("How many stacks will rot soon.")]
        public int TotalSoonRottingStacks { get; set; }
    }

    public class RottingFoodItemDto
    {
        public int ThingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How many units the stack holds.")]
        public int StackCount { get; set; }
        [ParameterDescription("How much nutrition the stack holds.")]
        public float Nutrition { get; set; }
        [ParameterDescription("How many days until it rots.")]
        public float DaysUntilRot { get; set; }
        [ParameterDescription("How many hours until it rots.")]
        public float HoursUntilRot { get; set; }
    }
}
