using System.Collections.Generic;
using System.Text;

namespace RIMAPI.Helpers
{
    // TradeHoldingText writes the sentence a refused trade answers with when the
    // colony's goods exist but the trade window cannot see them (fox ticket
    // #3494): 847 silver in two stacks, and every purchase refused with "the
    // colony has 0 silver". Which goods a trade counts is the game's own rule,
    // and the two kinds of trader differ, so the caller passes that rule in.
    //
    // Pure on purpose: the tests compile this file against the stubs, so the
    // wording is covered without a running game.
    public class TradeHoldingText
    {
        // Label is the word the sentence uses for the goods ("silver"), DefName
        // the defName a run has to pass to trade them.
        public string Label;
        public string DefName;

        // Total is how many units lie on the map, Counted how many of them the
        // trade window counts, Stacks how many stacks hold the total.
        public int Total;
        public int Counted;
        public int Stacks;

        // Places is one phrase per uncounted stack, Rule the game rule that
        // leaves them out ("an orbital trade counts only ...").
        public List<string> Places = new List<string>();
        public string Rule;

        // tradeHoldingPlaces caps the stacks a sentence names: a refusal is read
        // by a model that needs the reason and the fix, not an inventory.
        private const int tradeHoldingPlaces = 4;

        public bool HasOutside
        {
            get { return Stacks > 0 && Total > Counted; }
        }

        // Sentence is the clause to append to a refusal, empty when the trade
        // counted everything the map holds.
        public string Sentence()
        {
            if (!HasOutside)
            {
                return "";
            }
            StringBuilder b = new StringBuilder();
            b.Append("the colony holds ");
            b.Append(Total);
            b.Append(" ");
            b.Append(Label);
            b.Append(" in ");
            b.Append(Stacks);
            b.Append(Stacks == 1 ? " stack" : " stacks");
            b.Append(" on the map, but the trade counts ");
            b.Append(Counted);
            b.Append(" of it: ");
            int shown = Places.Count < tradeHoldingPlaces ? Places.Count : tradeHoldingPlaces;
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    b.Append(", ");
                }
                b.Append(Places[i]);
            }
            if (Places.Count > shown)
            {
                b.Append(" and ");
                b.Append(Places.Count - shown);
                b.Append(" more stacks");
            }
            if (!string.IsNullOrEmpty(Rule))
            {
                b.Append("; ");
                b.Append(Rule);
            }
            return b.ToString();
        }
    }
}
