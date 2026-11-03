using RIMAPI.Core;

namespace RIMAPI.Models
{
    // One animal standing on the map (fox ticket #3467 added everything below
    // Pregnant). Before it, this route named the animal but not which side it
    // is on, what the game has it marked for or whether it is a manhunter, so a
    // run that wanted a muffalo for wool could not call `tame`: the hunt it used
    // instead answered with a charge.
    public class AnimalDto
    {
        [ParameterDescription("The animal's numeric id, as the animal list reports it.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The animal's def name, e.g. \"Muffalo\".")]
        public string Def { get; set; }
        [ParameterDescription("The faction the animal belongs to, as the game names it.")]
        public string Faction { get; set; }
        public PositionDto Position { get; set; }
        [ParameterDescription("The numeric id of the colonist who trains it, when one does.")]
        public int? Trainer { get; set; }
        [ParameterDescription("Whether the animal is carrying a pregnancy.")]
        public bool Pregnant { get; set; }

        // Label is the race's own name ("muffalo"), where Name may be the
        // animal's given name.
        public string Label { get; set; }

        // Side is "tame" for an animal of the player's faction, the faction's
        // own name for anyone else's animal, and "wild" for a pawn of no
        // faction: only a wild animal is tamed.
        [ParameterDescription("Side is \"tame\" for an animal of the player's faction, the faction's own name for anyone else's animal, and \"wild\" for a pawn of no faction: only a wild animal is tamed.")]
        public string Side { get; set; }

        // Sex is "male", "female" or "none" (the game's Gender values), and
        // LifeStage the game's own label for the age ("adult", "baby").
        [ParameterDescription("Sex is \"male\", \"female\" or \"none\" (the game's Gender values), and LifeStage the game's own label for the age (\"adult\", \"baby\").")]
        public string Sex { get; set; }
        [ParameterDescription("The game's label for the animal's age: adult or baby.")]
        public string LifeStage { get; set; }

        [ParameterDescription("The animal's hit points, 0 up to its race's maximum.")]
        public float Health { get; set; }
        [ParameterDescription("Whether the animal lies down and cannot move.")]
        public bool Downed { get; set; }

        // Tameable is TameUtility.CanTame, the same check the tame order makes.
        [ParameterDescription("Tameable is TameUtility.CanTame, the same check the tame order makes.")]
        public bool Tameable { get; set; }

        // ManhunterOnDamageChance is the race's own chance to turn manhunter
        // when something hurts it (ThingDef manhunterOnDamageChance, 0..1; 0
        // when the def names none). A hunt of a cassowary (1.00) is a fight and
        // a hunt of an alpaca (0) is not, and the read said neither (fox
        // #4183). The game multiplies it by the difficulty's own
        // manhunterChanceOnDamageFactor at the moment of the wound.
        [ParameterDescription("ManhunterOnDamageChance is the race's own chance to turn manhunter when something hurts it (ThingDef manhunterOnDamageChance, 0..1; 0 when the def names none).")]
        public float ManhunterOnDamageChance { get; set; }

        // ManhunterOnTameFailChance is the race's own chance to turn manhunter
        // on each failed tame attempt (ThingDef manhunterOnTameFailChance, 0..1;
        // 0 when the def names none, fox #4191). A wolf is 0.5 and an alpaca 0,
        // so the same `tame` order is a coin flip on the first and harmless on
        // the second. The game rolls it on every failed attempt, once per try,
        // with no difficulty multiplier.
        [ParameterDescription("ManhunterOnTameFailChance is the race's own chance to turn manhunter on each failed tame attempt (ThingDef manhunterOnTameFailChance, 0..1; 0 when the def names none, fox #4191).")]
        public float ManhunterOnTameFailChance { get; set; }

        // The two designations a run sets and then looks for: the game keeps
        // them on the animal, not on a cell, so no map read of designations
        // tells a marked animal from an unmarked one.
        [ParameterDescription("The two designations a run sets and then looks for: the game keeps them on the animal, not on a cell, so no map read of designations tells a marked animal from an unmarked one.")]
        public bool TameDesignated { get; set; }
        [ParameterDescription("Whether the colony has marked it for hunting.")]
        public bool HuntDesignated { get; set; }

        // Manhunter is the game's own mental state: a herd in it attacks on
        // sight, which is what a hunt of the herd runs into. Predator is the
        // race's flag, so a warg reads as the danger it is before it attacks.
        [ParameterDescription("Manhunter is the game's own mental state: a herd in it attacks on sight, which is what a hunt of the herd runs into.")]
        public bool Manhunter { get; set; }
        [ParameterDescription("Whether the animal's race hunts other pawns for food.")]
        public bool Predator { get; set; }

        // QuestLodger is Pawn.IsQuestLodger: the animal stands on the map
        // because a quest sent it, so it counts as the colony's by faction and
        // still is not the colony's to kill. Colony 14 slaughtered such a sheep,
        // the quest failed and the guest faction dropped to -7 (fox #3611).
        [ParameterDescription("QuestLodger is Pawn.IsQuestLodger: the animal stands on the map because a quest sent it, so it counts as the colony's by faction and still is not the colony's to kill.")]
        public bool QuestLodger { get; set; }

        // CarriedBy names the pawn whose hands hold this animal, empty when
        // nobody does, and CarriedTo where that carrier's job is heading (fox
        // #3668). A carried pawn is despawned into the carrier's hands, so the
        // read used to drop it and a rescue looked like a death.
        [ParameterDescription("CarriedBy names the pawn whose hands hold this animal, empty when nobody does, and CarriedTo where that carrier's job is heading (fox #3668).")]
        public string CarriedBy { get; set; }
        [ParameterDescription("The cell the carrier is heading to, empty when nobody carries it.")]
        public string CarriedTo { get; set; }
    }
}
