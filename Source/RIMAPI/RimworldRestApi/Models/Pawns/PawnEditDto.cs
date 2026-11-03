using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // --- 1. Basic Info ---
    public class PawnBasicRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("The full name to give the pawn.")]
        public string Name { get; set; } // Fallback for simple name
        [ParameterDescription("The pawn's first name.")]
        public string FirstName { get; set; }
        [ParameterDescription("The pawn's last name.")]
        public string LastName { get; set; }
        [ParameterDescription("The pawn's nickname, as the game shows it.")]
        public string NickName { get; set; }
        [ParameterDescription("The gender to give the pawn.")]
        public string Gender { get; set; } // Male, Female, None
        [ParameterDescription("The pawn's biological age in years.")]
        public int? BiologicalAge { get; set; }
        [ParameterDescription("The pawn's chronological age in years.")]
        public int? ChronologicalAge { get; set; }
    }

    // --- 2. Health ---
    public class PawnHealthRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("When true, every injury is healed.")]
        public bool HealAllInjuries { get; set; }
        [ParameterDescription("When true, missing body parts grow back.")]
        public bool RestoreBodyParts { get; set; } // Regrow missing limbs
        [ParameterDescription("When true, every disease is cured.")]
        public bool RemoveAllDiseases { get; set; }
    }

    // --- 3. Needs ---
    public class PawnNeedsRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("The level to set the pawn's food need to, 0 to 1.")]
        public float? Food { get; set; } // 0.0 to 1.0
        [ParameterDescription("The level to set the pawn's rest need to, 0 to 1.")]
        public float? Rest { get; set; }
        [ParameterDescription("The level to set the pawn's mood to, 0 to 1.")]
        public float? Mood { get; set; }
    }

    // --- 4. Skills ---
    public class PawnSkillsRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("One entry per skill to set.")]
        public List<SkillEntryDto> Skills { get; set; }
    }

    public class SkillEntryDto
    {
        [ParameterDescription("The skill's def name, e.g. \"Shooting\".")]
        public string SkillName { get; set; } // e.g. "Shooting"
        [ParameterDescription("The level to set, 0 to 20.")]
        public int? Level { get; set; } // 0-20
        [ParameterDescription("The passion level: none, minor or major.")]
        public int? Passion { get; set; } // 0-2
    }

    // --- 5. Traits ---
    public class PawnTraitsRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("The traits to add, by def name.")]
        public List<TraitEntryDto> AddTraits { get; set; }
        [ParameterDescription("The traits to remove, by def name.")]
        public List<string> RemoveTraits { get; set; }
    }

    public class TraitEntryDto
    {
        [ParameterDescription("The trait's def name.")]
        public string TraitName { get; set; } // e.g. "SpeedOffset"
        [ParameterDescription("The trait's degree, for traits that have degrees.")]
        public int? Degree { get; set; } // Specific degree (e.g., -1 for Pessimist, 1 for Optimist)
    }

    // --- 6. Inventory ---
    public class PawnInventoryRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("When true, the pawn drops everything it carries.")]
        public bool DropInventory { get; set; }
        [ParameterDescription("When true, the carried items are deleted.")]
        public bool ClearInventory { get; set; } // Deletes items entirely
        [ParameterDescription("The items to put into the inventory.")]
        public List<ItemDto> AddItems { get; set; }
    }

    public class ItemDto
    {
        public string DefName { get; set; } // e.g. "MealSimple"
        public int Count { get; set; }
    }

    // --- 7. Apparel / Equipment ---
    public class PawnApparelRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("When true, the pawn drops its apparel.")]
        public bool DropApparel { get; set; } // Clothes
        [ParameterDescription("When true, the pawn drops its weapons.")]
        public bool DropWeapons { get; set; } // Equipment
    }

    // --- 8. Status ---
    public class PawnStatusRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("Whether the pawn is drafted.")]
        public bool? IsDrafted { get; set; }
        [ParameterDescription("When true, the pawn dies.")]
        public bool Kill { get; set; }
        [ParameterDescription("When true, a dead pawn comes back.")]
        public bool Resurrect { get; set; }
    }

    // --- 9. Position ---
    public class PawnPositionRequest
    {
        public int PawnId { get; set; }
        public string MapId { get; set; } // Optional: Move to another map
        public PositionDto Position { get; set; }
    }

    // --- 10. Faction ---
    public class PawnFactionRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("The faction to put the pawn in, by def name.")]
        public string SetFaction { get; set; } // Faction DefName
        [ParameterDescription("When true, the pawn joins the colony.")]
        public bool MakeColonist { get; set; } // Auto recruit
        [ParameterDescription("When true, the pawn becomes a prisoner.")]
        public bool MakePrisoner { get; set; }
        [ParameterDescription("When true, a prisoner is released.")]
        public bool ReleasePrisoner { get; set; }
    }
}