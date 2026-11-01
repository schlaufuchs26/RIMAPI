namespace RIMAPI.Helpers
{
    /// <summary>
    /// What makes a food stack a rot risk (fox ticket #3672). Colony 14 stored
    /// 59 horse meat in a room whose PassiveCooler held it at 16 C: cold for a
    /// person, warm for raw food, and the meat was gone by morning. A room
    /// above 0 C is the line, because the game's rot rate is zero at or below
    /// freezing and rises with the temperature; the hours the stack has left
    /// come from its own rot progress, not from the room alone.
    ///
    /// The checks stay free of game types so Source/RIMAPI.Tests can state
    /// them; RottingFood maps a Thing onto FoodState.
    /// </summary>
    public static class RottingFoodRules
    {
        /// <summary>One food stack, as the game reports it.</summary>
        public struct FoodState
        {
            /// <summary>The def gives nutrition to a human (IsNutritionGivingIngestible).</summary>
            public bool NutritionGiving;
            /// <summary>A corpse is not food the colony may eat, even though its def carries nutrition.</summary>
            public bool Corpse;
            /// <summary>The stack carries a rot comp, so it spoils at all.</summary>
            public bool Rottable;
            /// <summary>The stack stands in a room the room grid knows.</summary>
            public bool HasRoom;
            /// <summary>The temperature of that room in C.</summary>
            public float RoomTemperature;
        }

        /// <summary>Above this the game advances a stack's rot progress; at or below it, nothing rots.</summary>
        public const float FreezingC = 0f;

        /// <summary>
        /// True when the stack is perishable food that stands in a room warm
        /// enough for rot. A stack with no room (a caravan, an unspawned thing)
        /// is left out: the read cannot say what temperature it sees.
        /// </summary>
        public static bool IsAtRisk(FoodState food)
        {
            if (!food.NutritionGiving || food.Corpse) return false;
            if (!food.Rottable) return false;
            if (!food.HasRoom) return false;
            return food.RoomTemperature > FreezingC;
        }

        /// <summary>
        /// The game's rot estimate as hours, clamped so the answer is always a
        /// finite JSON number: an item that lasts longer than this reads as
        /// "not soon" instead of infinity, which no JSON reader accepts.
        /// </summary>
        public const float MaxHoursUntilRot = 100000f;

        public static float ClampHours(float hours)
        {
            if (float.IsNaN(hours) || hours < 0f) return 0f;
            if (float.IsInfinity(hours) || hours > MaxHoursUntilRot) return MaxHoursUntilRot;
            return hours;
        }
    }
}
