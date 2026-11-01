using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    // One stored stack the automatic feed lookup could not use, and why. The
    // lookup answered "no meal the patient may eat is in reach" while the
    // colony stored 16 meals, so the run cooked instead of feeding (fox
    // ticket #3680); a refusal now names the stacks it looked at.
    public readonly struct MealRefusal
    {
        public MealRefusal(string label, int id, int count, string reason)
        {
            Label = label;
            Id = id;
            Count = count;
            Reason = reason;
        }

        // Label is the stack's own name, e.g. "simple meal".
        public string Label { get; }

        // Id is the thing id a run passes as `item_id` to feed from this stack.
        public int Id { get; }

        public int Count { get; }

        // Reason is a predicate clause, e.g. "is forbidden", so the list reads
        // as one sentence per stack.
        public string Reason { get; }
    }

    // The words behind a feed refusal, free of game types so
    // Source/RIMAPI.Tests can state them. The game calls live in
    // ColonistService.Feed.
    public static class FeedRules
    {
        // A refusal lists a few stacks, not all of them: colony 14 stored 16
        // meals in two stacks, but a big store can hold dozens.
        public const int MaxListedStacks = 4;

        // NotHungry explains the gate the game itself applies: no feed job
        // while the patient still has food. The incident behind ticket #3680
        // was this gate, not a missing meal, and the old answer sent the run
        // off to cook.
        public static string NotHungry(string patient, float levelPercent, float thresholdPercent, int mealCount,
            int mealStacks)
        {
            string text = patient + " is not hungry yet: food " + Percent(levelPercent)
                + "%, and the game hands out a feed job only at " + Percent(thresholdPercent) + "% or below";
            if (mealCount <= 0)
            {
                return text + ", so wait rather than cook";
            }
            string stacks = mealStacks == 1 ? "one stack" : mealStacks + " stacks";
            return text + "; the colony stores " + mealCount + " meals in " + stacks
                + ", so there is nothing to cook - `feed` works again once the patient is hungry";
        }

        // NoMealRefusal answers a patient who needs feeding and cannot get any
        // of the stored food. It names each stack with its reason, so the run
        // knows whether to allow, carry or cook instead of guessing.
        public static string NoMealRefusal(IReadOnlyList<MealRefusal> considered)
        {
            if (considered == null || considered.Count == 0)
            {
                return "the map stores no food at all, so there is nothing to allow or carry: cook a meal "
                    + "(`bill`) or harvest a field";
            }
            var parts = new List<string>();
            int shown = considered.Count < MaxListedStacks ? considered.Count : MaxListedStacks;
            for (int i = 0; i < shown; i++)
            {
                MealRefusal refusal = considered[i];
                parts.Add(refusal.Label + " x" + refusal.Count + " (id " + refusal.Id + ") " + refusal.Reason);
            }
            string more = considered.Count > shown ? "; and " + (considered.Count - shown) + " more" : "";
            return "none of the " + considered.Count + " stored food stacks can be used: " + string.Join("; ", parts)
                + more + ". Allow a forbidden stack (`allow`), carry it inside the home area, or cook a meal the "
                + "patient may eat (`bill`)";
        }

        private static string Percent(float value)
        {
            return ((int)System.Math.Round(value)).ToString();
        }
    }
}
