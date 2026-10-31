using System;
using System.Reflection;
using Verse;

namespace RIMAPI.Helpers
{
    // The text an archive entry carries, for both archive reads (tickets #3147,
    // #3336, #3400): the letter stack's bodies and the message archive's
    // entries. Letter itself declares no body, StandardLetter and ChoiceLetter
    // each hold their own, so the members are looked up by name through the
    // type hierarchy; a name this game build does not have yields null instead
    // of an exception.
    //
    // It lives here rather than in one service because two services read the
    // same archive: the letters read answers the stack, the messages read
    // answers the whole archive including its letters (ticket #3400).
    internal static class ArchiveText
    {
        // LetterBody is the body the game would show when the letter is opened:
        // its own text, or the tooltip text for a letter without one.
        public static string LetterBody(Letter letter)
        {
            var text = Resolve(Read(letter, "Text"));
            if (!string.IsNullOrEmpty(text))
                return text;
            return Resolve(Call(letter, "GetMouseoverText"));
        }

        // FirstLine keeps one line: a letter body is a paragraph, and the reads
        // are lists of them.
        public static string FirstLine(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var line = s.Replace("\r", "").Split('\n')[0].Trim();
            if (line.Length > 300)
                line = line.Substring(0, 300) + "...";
            return line;
        }

        // Resolve turns a member's value into a string: a TaggedString is
        // resolved to what the game would draw, everything else via ToString.
        public static string Resolve(object value)
        {
            if (value == null)
                return "";
            var tagged = value as TaggedString?;
            if (tagged.HasValue)
                return tagged.Value.Resolve();
            return value.ToString();
        }

        // Read looks a property or field up by name up the type hierarchy, the
        // way the window read does: the members these reads need are private or
        // protected in Verse.
        public static object Read(object obj, string name)
        {
            for (var type = obj.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(obj, null);
                var field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(obj);
            }
            return null;
        }

        // Call runs a parameterless method found the same way.
        public static object Call(object obj, string name)
        {
            for (var type = obj.GetType(); type != null; type = type.BaseType)
            {
                var method = type.GetMethod(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null, Type.EmptyTypes, null);
                if (method != null)
                    return method.Invoke(obj, null);
            }
            return null;
        }
    }
}
