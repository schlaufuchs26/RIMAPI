namespace RIMAPI.Helpers
{
    // The rules behind the slaughter order (fox ticket #3507): a colony animal
    // that is tamed for wool stays alive until it dies of age, because the hunt
    // designation only reaches wild animals and nothing else ends an animal's
    // life on purpose. The checks stay free of game types so Source/RIMAPI.Tests
    // can state them; the order itself lives in ColonistService.
    public static class SlaughterRules
    {
        /// <summary>One animal's state, as the game reports it.</summary>
        public struct AnimalState
        {
            public bool Animal;
            public bool Dead;
            public bool PlayerFaction;
            /// <summary>The owning faction's name, empty for a wild animal.</summary>
            public string FactionName;
            /// <summary>The mental state's name ("manhunter"), empty when sane.</summary>
            public string MentalState;
            /// <summary>
            /// The animal stands here because a quest sent it: the colony's by
            /// faction, and killing it fails that quest (fox #3611).
            /// </summary>
            public bool QuestLodger;
        }

        /// <summary>
        /// The first condition that keeps the animal from being slaughtered, as
        /// a phrase that reads after "Bess (id 9001) cannot be slaughtered: ",
        /// or null when the order can go out. A wild animal leads to the hunt
        /// designation, which is the order that does apply to it.
        /// </summary>
        public static string Refusal(AnimalState animal)
        {
            if (!animal.Animal)
                return "only animals are slaughtered; a humanlike pawn is arrested or rescued";
            if (animal.Dead) return "he is already dead";
            if (animal.QuestLodger)
                return "he is a quest guest, so killing him fails the quest: leave him alive until it ends";
            if (!animal.PlayerFaction)
            {
                if (string.IsNullOrEmpty(animal.FactionName))
                    return "he is wild, so he is not the colony's to slaughter: `designate` with type=hunt marks him for a hunter";
                return "he belongs to " + animal.FactionName + ", so he is not the colony's to slaughter";
            }
            if (!string.IsNullOrEmpty(animal.MentalState))
                return "he is in a mental state (" + animal.MentalState + "), so no handler can take him";
            return null;
        }
    }
}
