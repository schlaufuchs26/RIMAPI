using Verse;

namespace RIMAPI.Helpers
{
    // The body one letter answers with (tickets #3491, #3881). It lives here
    // rather than in LetterService because the service file is already past the
    // size gate, and the rule it applies is plain: the whole body, not the first
    // line, plus the quest's own description for a quest offer.
    internal static class LetterBodyText
    {
        // MaxBodyChars caps the body one letter answers with, so a long
        // narrative letter cannot fill a wait report.
        private const int MaxBodyChars = 4000;

        // Of is the whole body, not the first line: the wait report hands a run
        // the letter it arrived on, and a first line of a quest offer says
        // nothing about the reward (ticket #3491). A quest offer's own text only
        // announces the offer and names the quest; the quest's own description
        // is what says which threat and which game condition it brings, so the
        // body carries both (ticket #3881).
        public static string Of(Letter letter)
        {
            var body = ArchiveText.LetterBody(letter);
            var offer = letter as ChoiceLetter;
            if (offer != null && offer.quest != null)
                body = QuestOfferText.Body(body, ArchiveText.Resolve(offer.quest.description));
            if (string.IsNullOrEmpty(body))
                return "";
            body = body.Replace("\r", "").Trim();
            if (body.Length > MaxBodyChars)
                body = body.Substring(0, MaxBodyChars) + "...";
            return body;
        }
    }
}
