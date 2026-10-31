using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    // Which colonist a bill's skill line should name (ticket #3371). A colony
    // with a cook bill that runs and no meals in stock has two possible
    // stories: the kitchen is broken, or the cook is too slow. The second one
    // is invisible without a number, and "the cook has Cooking 0" is that
    // number: a level-0 cook works at a fraction of the speed and eats what he
    // makes.
    //
    // Game-free: the caller gathers the candidates (every free colonist's level
    // in the recipe's work skill, and whether his work settings include the
    // bench's work type) and this picks the one to name, so the choice is
    // tested without the game.
    public static class SkillPicker
    {
        public struct Candidate
        {
            public string Name;
            public int Level;
            // Works says the colonist's work settings include the bench's work
            // type. When the caller knows no work type for the bench, it passes
            // true for everybody.
            public bool Works;
        }

        public struct Best
        {
            public string Name;
            public int Level;
        }

        // Pick answers the highest level among the colonists who work the
        // bench, and the one holding it (the first of equals, so the answer is
        // stable across calls). Nobody working the type falls back to the
        // highest level of anybody: the line then still says what the colony
        // has, and the bill's own state names the assignment as the problem.
        public static Best Pick(IList<Candidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return new Best { Name = null, Level = 0 };
            Best best = new Best { Name = null, Level = int.MinValue };
            bool anyWorks = false;
            foreach (Candidate c in candidates)
            {
                if (c.Works)
                    anyWorks = true;
            }
            foreach (Candidate c in candidates)
            {
                if (anyWorks && !c.Works)
                    continue;
                if (c.Level > best.Level)
                    best = new Best { Name = c.Name, Level = c.Level };
            }
            return best;
        }
    }
}
