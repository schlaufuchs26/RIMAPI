namespace RIMAPI.Helpers
{
    // What no report of the message archive prints (ticket #3428): the game's
    // own bookkeeping, the alert text another read already carries, and the
    // idle notes that toggle as pawns pick up work. The archive route drops
    // these before it applies its limit, so a caller that asks for the newest
    // 25 entries gets 25 it can show. Without that it had to ask for four times
    // as many and still answered fewer real lines, because a save, a raid and
    // a few idle pawns filled the newest entries alone.
    //
    // The rule is the one the fox's Go read applies after the call
    // (core/rimworld/messages.go, MessageNoise). Both lists stay in step: a def
    // or text added on one side alone lets the other print a line the report
    // does not want, and the save-backed fallback on that side still needs it.
    internal static class ArchiveNoise
    {
        // IsNoise reports whether a message entry is one a report should not
        // repeat. Letters never pass through here: their class is their own and
        // a death letter is where a death's cause is written.
        public static bool IsNoise(string defName, string text)
        {
            if (defName == "SilentInput")
                return true;

            var line = (text ?? "").Trim().ToLowerInvariant();
            if (line.Length == 0)
                return true;
            return line.StartsWith("critical alert:")
                || line.StartsWith("game saved as:")
                || line.Contains("colonist is idle")
                || line.Contains("colonists are idle");
        }
    }
}
