using System;
using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The material half of a planned build's refusal (fox ticket #4054). The
    /// game counts only the stacks a builder may haul: a forbidden stack and a
    /// stack he cannot walk to are no material, so `prioritize` on a plan read
    /// "0 on the map he may use" while the colony sat on the stack. The fix is
    /// the one the bill reason took for ingredients (ticket #3984): count what
    /// the game can haul, and name the forbidden stack with its thing ids and
    /// the call that lifts the flag.
    /// No RimWorld type appears here, so RIMAPI.Tests covers the wording.
    /// </summary>
    public static class ConstructMaterialRules
    {
        /// <summary>
        /// MaterialGap is one material a plan needs, split the way a builder
        /// sees it: Usable is what he can haul, Forbidden is what the flag keeps
        /// out (with the thing ids `allow` takes) and OutOfReach is what he
        /// cannot walk to.
        /// </summary>
        public class MaterialGap
        {
            public string DefName;
            public string Label;
            public int Needed;
            public int Usable;
            public int Forbidden;
            public List<int> ForbiddenIds = new List<int>();
            public int OutOfReach;
        }

        /// <summary>
        /// Shortfall is the sentence after "The game counts no usable material
        /// for the Wall at 10,10: ", or null when the haulable stock covers
        /// every material. One clause per short material, joined with "; ", so
        /// two materials never look like one.
        /// </summary>
        public static string Shortfall(IList<MaterialGap> gaps)
        {
            if (gaps == null)
            {
                return null;
            }
            List<string> parts = new List<string>();
            foreach (MaterialGap gap in gaps)
            {
                if (gap == null || gap.Needed <= 0 || gap.Usable >= gap.Needed)
                {
                    continue;
                }
                parts.Add(GapText(gap));
            }
            return parts.Count > 0 ? string.Join("; ", parts) : null;
        }

        /// <summary>
        /// GapText names one short material: what the plan needs, what he may
        /// use, then what lies on the map but stays out of his hands. The
        /// forbidden half carries the ids because `rimworld_order action=allow
        /// ids=` is the one call that ends it; a stack the mod reports without
        /// an id stops the sentence at the stack instead of naming a call that
        /// could not reach it.
        /// </summary>
        public static string GapText(MaterialGap gap)
        {
            string label = string.IsNullOrEmpty(gap.Label) ? gap.DefName : gap.Label;
            string text = string.Format("{0} {1} needed, {2} on the map he may use",
                gap.Needed, label, gap.Usable);
            if (gap.Forbidden > 0)
            {
                string defName = string.IsNullOrEmpty(gap.DefName) ? label : gap.DefName;
                string stacks = StackList(gap.ForbiddenIds);
                text += string.Format("; forbidden: {0} {1}", gap.Forbidden, defName);
                if (stacks != null)
                {
                    text += " (" + stacks + ")";
                    text += "; action allow ids=" + string.Join(",", gap.ForbiddenIds) + " clears the flag";
                }
            }
            if (gap.OutOfReach > 0)
            {
                string defName = string.IsNullOrEmpty(gap.DefName) ? label : gap.DefName;
                text += string.Format("; {0} {1} lie out of his reach", gap.OutOfReach, defName);
            }
            return text;
        }

        // StackCap keeps the clause one sentence: a def split over a dozen
        // stacks lists the first three ids and counts the rest, the budget the
        // bill reason uses for the same line.
        private const int StackCap = 3;

        // StackList renders the thing ids of the forbidden stacks: "(thing 3)",
        // "(things 3, 7)" or, past the cap, "(things 3, 7, 9 and 4 more)". Null
        // when no stack carries an id: the caller then stops the clause at the
        // stack, because no `allow` call could reach it.
        private static string StackList(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return null;
            }
            List<string> shown = new List<string>();
            for (int i = 0; i < ids.Count && i < StackCap; i++)
            {
                shown.Add(ids[i].ToString());
            }
            string text = ids.Count == 1 ? "thing " : "things ";
            text += string.Join(", ", shown);
            int more = ids.Count - shown.Count;
            if (more > 0)
            {
                text += string.Format(" and {0} more", more);
            }
            return text;
        }
    }
}
