

using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ItemRecipesDto
    {
        [ParameterDescription("The item's def name.")]
        public string ItemDefName { get; set; }
        [ParameterDescription("The item's label, as the game shows it.")]
        public string ItemLabel { get; set; }
        [ParameterDescription("The recipes that make the item.")]
        public List<ThingRecipeDto> Recipes { get; set; }
    }

    public class ThingRecipeDto
    {
        [ParameterDescription("The recipe's def name.")]
        public string RecipeDefName { get; set; }
        public string Label { get; set; } // e.g. "Cook simple meal"
        [ParameterDescription("The sentence the bill shows while it runs.")]
        public string JobString { get; set; } // e.g. "Cooking simple meal."
        [ParameterDescription("How much work the recipe takes.")]
        public float WorkAmount { get; set; }
        [ParameterDescription("How many seconds that work takes at normal speed.")]
        public float WorkTimeSeconds { get; set; } // Calculated (WorkAmount / 60)

        [ParameterDescription("The ingredients one run of the recipe takes.")]
        public List<RecipeIngredientDto> Ingredients { get; set; }
        [ParameterDescription("The work tables that run the recipe.")]
        public List<RecipeProducerDto> ProducedAt { get; set; }
        [ParameterDescription("The skill levels the crafter needs.")]
        public List<RecipeSkillDto> SkillRequirements { get; set; }

        [ParameterDescription("The project that must finish first.")]
        public string ResearchPrerequisite { get; set; }
    }

    public class RecipeIngredientDto
    {
        [ParameterDescription("The ingredient as one sentence.")]
        public string Summary { get; set; } // e.g. "Raw food", "Steel"
        public float Count { get; set; }
        [ParameterDescription("Whether the ingredient is one fixed def.")]
        public bool IsFixedItem { get; set; } // True if only 1 specific item is allowed
        [ParameterDescription("The defs the ingredient accepts, by name.")]
        public List<string> AllowedDefNames { get; set; } // List of valid items for this slot
    }

    public class RecipeProducerDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
    }

    public class RecipeSkillDto
    {
        [ParameterDescription("The skill the requirement asks for.")]
        public string Skill { get; set; }
        [ParameterDescription("The lowest level that satisfies it.")]
        public int MinLevel { get; set; }
    }
}