using System.Collections.Generic;

namespace RIMAPI.Models
{
    public class ResourcesSummaryDto
    {
        public int TotalItems { get; set; }
        public double TotalMarketValue { get; set; }
        public string LastUpdated { get; set; }
        public List<ResourceCategoryDto> Categories { get; set; }
        public CriticalResourcesDto CriticalResources { get; set; }
    }

    public class StoragesSummaryDto
    {
        public int TotalStockpiles { get; set; }
        public int TotalCells { get; set; }
        public int UsedCells { get; set; }
        public int UtilizationPercent { get; set; }
    }

    public class ResourceCategoryDto
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public double MarketValue { get; set; }
    }

    public class CriticalResourcesDto
    {
        public ResourcesFoodSummaryDto FoodSummary { get; set; }
        public int MedicineTotal { get; set; }
        public int WeaponCount { get; set; }
        public double WeaponValue { get; set; }

        // FuelSummary is the wood and the tanks (fox ticket #3678). A colony
        // ran its stove dry without a single read saying so: the stock line
        // listed food, medicine and weapons, and cooking simply stopped.
        public FuelSummaryDto FuelSummary { get; set; }
    }

    // FuelSummaryDto is the colony's fuel: what lies loose on the map, and
    // every building whose tank holds it. Wood is both the fuel and the
    // cheapest building material, so it is named even at zero.
    public class FuelSummaryDto
    {
        public List<FuelStockDto> Stocks { get; set; } = new List<FuelStockDto>();
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
    public class FuelBuildingDto
    {
        public int Id { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public float Fuel { get; set; }
        public float FuelCapacity { get; set; }
        public string FuelType { get; set; }
        public bool HasFuel { get; set; }
        public int Bills { get; set; }
        public int MealBills { get; set; }
    }

    public class ResourcesFoodSummaryDto
    {
        public int FoodTotal { get; set; }
        public float TotalNutrition { get; set; }
        public int MealsCount { get; set; }
        public int RawFoodCount { get; set; }
        public RotStatusInfoDto RotStatusInfo { get; set; }
    }

    public class RotStatusInfoDto
    {
        public float NutritionRotatingSoon { get; set; }
        public float NutritionNotRotating { get; set; }
        public float PercentageRotatingSoon { get; set; }
        public List<RottingFoodItemDto> SoonRottingItems { get; set; } =
            new List<RottingFoodItemDto>();
        public int TotalSoonRottingItems { get; set; }
        public int TotalSoonRottingStacks { get; set; }
    }

    public class RottingFoodItemDto
    {
        public int ThingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public int StackCount { get; set; }
        public float Nutrition { get; set; }
        public float DaysUntilRot { get; set; }
        public float HoursUntilRot { get; set; }
    }
}
