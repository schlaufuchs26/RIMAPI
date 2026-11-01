using System;
using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    // The name a colony's new ideoligion gets (fox ticket #3929). The fast
    // start builds its own fluid ideo, and until now the game left it with no
    // name at all: every colony's belief read as "" in the ideo route, and a
    // later `newcolony ideo=<name>` could not pick it either. This holds the
    // checks that need no game type.
    public static class IdeoNameRules
    {
        // The game's own message for an empty name (Page_ConfigureIdeo checks
        // it with this key), so a refusal speaks the game's words.
        public const string EmptyKey = "MessageIdeoNameCantBeEmpty";

        // The naming dialogs' own limit; an ideo name longer than this reads
        // as a paragraph in every line that carries it.
        public const int CharacterLimit = 64;

        // Check returns null when the name can be used, else the reason as a
        // sentence. existing is every ideo name the game keeps; a second ideo
        // with the same name would make `newcolony ideo=<name>` pick the wrong
        // one. emptyMessage is the game's translated text for an empty name,
        // so the caller keeps the game's own wording.
        public static string Check(string name, IEnumerable<string> existing, string emptyMessage)
        {
            string trimmed = name == null ? "" : name.Trim();
            if (trimmed.Length == 0)
            {
                return string.IsNullOrEmpty(emptyMessage)
                    ? "the ideoligion name can't be empty"
                    : emptyMessage;
            }
            if (trimmed.Length > CharacterLimit)
                return "the ideoligion name is longer than " + CharacterLimit + " characters";
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (trimmed[i] < ' ')
                    return "the ideoligion name carries a control character, which no line about the colony can print";
            }
            if (existing != null)
            {
                foreach (string other in existing)
                {
                    if (other == null)
                        continue;
                    if (string.Equals(other.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        return "the game already keeps an ideoligion named \"" + other.Trim() +
                               "\", and a second one with the same name would make `ideo=<name>` ambiguous";
                    }
                }
            }
            return null;
        }
    }
}
