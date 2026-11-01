namespace RIMAPI.Helpers
{
    // The body a quest offer carries in the letters read (ticket #3881). The
    // game's own offer letter says only that a quest is available and what it is
    // called, while the quest's own description names the threat, the game
    // condition and what the quest asks for. A run that has to decide
    // accept-or-refuse from the letter needs that text, so the read hands both:
    // the letter's own words first, the quest's description after them.
    public static class QuestOfferText
    {
        public static string Body(string letterBody, string questDescription)
        {
            var letter = (letterBody ?? "").Replace("\r", "").Trim();
            var description = (questDescription ?? "").Replace("\r", "").Trim();
            if (description.Length == 0)
                return letter;
            if (letter.Length == 0)
                return description;
            return letter + "\n\n" + description;
        }
    }
}
