using System.Collections.Generic;
using RimWorld;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class FactionsDto
    {
        [ParameterDescription("The faction's load id, which survives a save.")]
        public int LoadId { get; set; }
        public string DefName { get; set; }
        public string Name { get; set; }
        [ParameterDescription("Whether this is the player's faction.")]
        public bool IsPlayer { get; set; }
        [ParameterDescription("How the player's faction stands to it: ally, neutral or hostile.")]
        public string Relation { get; set; }
        [ParameterDescription("The goodwill between it and the player, -100 to 100.")]
        public int Goodwill { get; set; }
    }

    public class FactionDto
    {
        [ParameterDescription("The faction's load id, which survives a save.")]
        public int LoadId { get; set; }
        public string DefName { get; set; }
        public string Name { get; set; }
        [ParameterDescription("Whether this is the player's faction.")]
        public bool IsPlayer { get; set; }
        [ParameterDescription("The title its leader carries, e.g. chief.")]
        public string LeaderTitle { get; set; }
        [ParameterDescription("The numeric id of its leader.")]
        public int LeaderId { get; set; }

        public static FactionDto ToDto(Faction f)
        {
            if (f == null)
                return null;
            int leaderId = f.leader != null ? f.leader.thingIDNumber : 0;

            return new FactionDto
            {
                LoadId = f.loadID,
                DefName = f.def?.defName,
                Name = f.Name,
                IsPlayer = f.IsPlayer,
                LeaderTitle = f.LeaderTitle,
                LeaderId = leaderId,
            };
        }
    }

    public class FactionRelationDto
    {
        [ParameterDescription("The goodwill between the two, -100 to 100.")]
        public int Goodwill { get; set; }
        [ParameterDescription("How the two stand to each other: ally, neutral or hostile.")]
        public string RelationKind { get; set; }
    }

    public class FactionChangeRelationRequestDto
    {
        [ParameterDescription("The faction's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("The other faction's numeric id.")]
        public int OtherId { get; set; }
        [ParameterDescription("How far to move the goodwill, in points.")]
        public int Value { get; set; }
        [ParameterDescription("Whether the game writes a message about it.")]
        public bool SendMessage { get; set; }
        [ParameterDescription("Whether a hostility letter may go out.")]
        public bool CanSendHostilityLetter { get; set; }
    }

    public class FactionRelationsDto
    {
        [ParameterDescription("The faction's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("One entry per faction it has a relation with.")]
        public Dictionary<int, FactionRelationDto> Relations { get; set; }
    }

    public class FactionChangeRelationResponceDto
    {
        [ParameterDescription("The faction's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("The other faction's numeric id.")]
        public int OtherId { get; set; }
        [ParameterDescription("The goodwill after the change, in points.")]
        public int Value { get; set; }
    }

    public class FactionDefDto
    {
        public string DefName { get; set; }
        [ParameterDescription("What one of its members is called, singular.")]
        public string PawnSingular { get; set; }
        [ParameterDescription("What its members are called, plural.")]
        public string PawnsPlural { get; set; }
        [ParameterDescription("The title its leader carries, e.g. chief.")]
        public string LeaderTitle { get; set; }
        [ParameterDescription("The leader title when the leader is female.")]
        public string LeaderTitleFemale { get; set; }
        [ParameterDescription("The category the game files it under.")]
        public string CategoryTag { get; set; }
        [ParameterDescription("Whether it attacks humanlikes with no faction.")]
        public bool HostileToFactionlessHumanlikes { get; set; }
        [ParameterDescription("How many settlements it starts with.")]
        public string StartingCountAtWorldCreation { get; set; }
        [ParameterDescription("Whether it can never make peace with the player.")]
        public bool PermanentEnemy { get; set; }
        [ParameterDescription("Whether it is hostile from the start.")]
        public bool NaturalEnemy { get; set; }
        [ParameterDescription("Whether it always runs the classic ideoligion.")]
        public bool ClassicIdeo { get; set; }
        [ParameterDescription("Whether its ideoligion stays hidden.")]
        public bool HiddenIdeo { get; set; }
        [ParameterDescription("The ideoligion it follows, by name.")]
        public string IdeoName { get; set; }
        [ParameterDescription("Whether it may lay siege to the colony.")]
        public bool CanSiege { get; set; }
        [ParameterDescription("Whether it may stage attacks before striking.")]
        public bool CanStageAttacks { get; set; }
        [ParameterDescription("Whether it may path around the colony's defences.")]
        public bool CanUseAvoidGrid { get; set; }
        [ParameterDescription("Whether it may use psychic ritual sieges.")]
        public bool CanPsychicRitualSiege { get; set; }
        [ParameterDescription("The earliest day it may raid, in game days.")]
        public float EarliestRaidDays { get; set; }

        public static FactionDefDto FromFactionDef(FactionDef f)
        {
            if (f == null)
                return null;

            return new FactionDefDto
            {
                DefName = f.defName,
                PawnSingular = f.pawnSingular,
                PawnsPlural = f.pawnsPlural,
                LeaderTitle = f.leaderTitle,
                LeaderTitleFemale = f.leaderTitleFemale,
                CategoryTag = f.categoryTag,
                HostileToFactionlessHumanlikes = f.hostileToFactionlessHumanlikes,
                StartingCountAtWorldCreation = f.startingCountAtWorldCreation.ToString() ?? "0",
                PermanentEnemy = f.permanentEnemy,
                NaturalEnemy = f.naturalEnemy,
                ClassicIdeo = f.classicIdeo,
                HiddenIdeo = f.hidden,
                IdeoName = f.ideoName,
                CanSiege = f.canSiege,
                CanStageAttacks = f.canStageAttacks,
                CanUseAvoidGrid = f.canUseAvoidGrid,
                CanPsychicRitualSiege = f.canPsychicRitualSiege,
                EarliestRaidDays = f.earliestRaidDays,
            };
        }
    }
}
