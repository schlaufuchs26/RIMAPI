using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    // What a naming dialog is given and which of its fields a call names (fox
    // ticket #3929). The faction, settlement and gravship dialogs
    // (Dialog_GiveName and its subclasses) draw their OK inline; the window
    // service writes the wanted names into curName / curSecondName and then
    // runs the game's own validation and Named / NamedSecond. This helper
    // holds the parts that need no game type, so they can be tested without
    // RimWorld: which name a call means, which field a refusal is about, and
    // the sentence the refusal prints.
    public static class NameDialogRules
    {
        // The game's own limit for both fields (Dialog_GiveName.FirstCharLimit
        // and SecondCharLimit, NamePlayerFactionDialogUtility.CharacterLimit).
        public const int CharacterLimit = 64;

        // Resolve is the name a dialog should end up with: the one the call
        // asked for, else the one the dialog already holds, which is the name
        // the game generated. Both are trimmed, because the game trims before
        // it validates.
        public static string Resolve(string wanted, string current)
        {
            if (wanted != null && wanted.Trim().Length > 0)
                return wanted.Trim();
            return current == null ? "" : current.Trim();
        }

        // Plan is what one accept call sets on a naming dialog, in the order
        // the dialog asks its fields. A dialog that wants one name takes the
        // caller's `name`, else its `settlement` (a run that thinks of the
        // colony's name has no reason to know which of the game's dialogs it
        // is answering), else the name the dialog holds. problem is null when
        // the call can go ahead.
        public static List<string> Plan(bool useSecond, string name, string second,
            string current, string currentSecond, out string problem)
        {
            problem = null;
            var names = new List<string>();
            if (!useSecond)
            {
                names.Add(Resolve(FirstNonEmpty(name, second), current));
                return names;
            }
            names.Add(Resolve(name, current));
            names.Add(Resolve(second, currentSecond));
            return names;
        }

        // FieldLabel names the dialog field a refusal is about, in the words
        // the game's own dialog uses.
        public static string FieldLabel(bool second, bool twoFields)
        {
            if (!twoFields)
                return "name";
            return second ? "settlement name" : "faction name";
        }

        // Refusal is the sentence a rejected name prints: the field, the name
        // the caller asked for, and the game's own reason. The game's reason
        // is its invalidNameMessageKey text ("That name isn't valid."), so a
        // refusal says what the dialog itself would have said.
        public static string Refusal(string field, string name, string gameReason)
        {
            string reason = string.IsNullOrEmpty(gameReason) || gameReason.Trim().Length == 0
                ? "the game gave no reason"
                : gameReason.Trim();
            if (name == null || name.Length == 0)
                return "the " + field + " field is empty: " + reason;
            return "the dialog rejects the " + field + " \"" + name + "\": " + reason;
        }

        private static string FirstNonEmpty(string first, string second)
        {
            if (first != null && first.Trim().Length > 0)
                return first;
            return second;
        }
    }
}
