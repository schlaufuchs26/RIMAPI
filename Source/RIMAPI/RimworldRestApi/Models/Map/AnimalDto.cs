namespace RIMAPI.Models
{
    // One animal standing on the map (fox ticket #3467 added everything below
    // Pregnant). Before it, this route named the animal but not which side it
    // is on, what the game has it marked for or whether it is a manhunter, so a
    // run that wanted a muffalo for wool could not call `tame`: the hunt it used
    // instead answered with a charge.
    public class AnimalDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Def { get; set; }
        public string Faction { get; set; }
        public PositionDto Position { get; set; }
        public int? Trainer { get; set; }
        public bool Pregnant { get; set; }

        // Label is the race's own name ("muffalo"), where Name may be the
        // animal's given name.
        public string Label { get; set; }

        // Side is "tame" for an animal of the player's faction, the faction's
        // own name for anyone else's animal, and "wild" for a pawn of no
        // faction: only a wild animal is tamed.
        public string Side { get; set; }

        // Sex is "male", "female" or "none" (the game's Gender values), and
        // LifeStage the game's own label for the age ("adult", "baby").
        public string Sex { get; set; }
        public string LifeStage { get; set; }

        public float Health { get; set; }
        public bool Downed { get; set; }

        // Tameable is TameUtility.CanTame, the same check the tame order makes.
        public bool Tameable { get; set; }

        // The two designations a run sets and then looks for: the game keeps
        // them on the animal, not on a cell, so no map read of designations
        // tells a marked animal from an unmarked one.
        public bool TameDesignated { get; set; }
        public bool HuntDesignated { get; set; }

        // Manhunter is the game's own mental state: a herd in it attacks on
        // sight, which is what a hunt of the herd runs into. Predator is the
        // race's flag, so a warg reads as the danger it is before it attacks.
        public bool Manhunter { get; set; }
        public bool Predator { get; set; }

        // QuestLodger is Pawn.IsQuestLodger: the animal stands on the map
        // because a quest sent it, so it counts as the colony's by faction and
        // still is not the colony's to kill. Colony 14 slaughtered such a sheep,
        // the quest failed and the guest faction dropped to -7 (fox #3611).
        public bool QuestLodger { get; set; }

        // CarriedBy names the pawn whose hands hold this animal, empty when
        // nobody does, and CarriedTo where that carrier's job is heading (fox
        // #3668). A carried pawn is despawned into the carrier's hands, so the
        // read used to drop it and a rescue looked like a death.
        public string CarriedBy { get; set; }
        public string CarriedTo { get; set; }
    }
}
