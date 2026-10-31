using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RIMAPI.Core;
using RIMAPI.Models;
using Verse;

namespace RIMAPI.Services
{
    // The game's letter stack (top right): the "Visitors", "Quest available",
    // "Raid" and "Research finished" notes. They are neither alerts nor
    // windows, so before this read nothing in the bridge could see them, and
    // they piled up on top of the map (ticket #3147).
    //
    // Reading and dismissing are the two things a caller needs: what stands
    // (title, body, kind, tick) and the game's own dismiss, so a letter that
    // was read stops covering the map.
    public class LetterService : ILetterService
    {
        public ApiResult<List<LetterDto>> ListLetters()
        {
            try
            {
                var standing = StackLetters();
                // The game keeps no read flag, so the bridge remembers what it
                // has reported: this call answers "new" for the letters no
                // earlier read showed and marks the whole stack seen
                // (ticket #3156).
                var fresh = LetterSeen.Report(standing.Select(l => l.ID));
                var letters = new List<LetterDto>(standing.Count);
                foreach (var letter in standing)
                {
                    var dto = ToDto(letter);
                    dto.New = fresh.Contains(letter.ID);
                    letters.Add(dto);
                }
                return ApiResult<List<LetterDto>>.Ok(letters);
            }
            catch (Exception ex)
            {
                return ApiResult<List<LetterDto>>.Fail(ex.Message);
            }
        }

        public ApiResult<LetterDismissResultDto> DismissLetter(LetterDismissRequestDto request)
        {
            try
            {
                if (request == null)
                    return ApiResult<LetterDismissResultDto>.Fail("the request needs the letter's id");
                var stack = Find.LetterStack;
                if (stack == null)
                    return ApiResult<LetterDismissResultDto>.Fail("no letter stack: no colony is loaded");
                var letter = StackLetters().FirstOrDefault(l => l.ID == request.Id);
                if (letter == null)
                    return ApiResult<LetterDismissResultDto>.Fail(
                        $"no letter with id {request.Id} stands; the letters read prints the ids");

                var result = new LetterDismissResultDto { Label = letter.Label };
                // A letter that needs an answer is not dismissed: the game
                // shows its popup instead, which holds the clock until a button
                // is pressed. Saying so is what sends the caller to the dialog
                // read, and dropping it silently would throw the offer away.
                if (!letter.CanDismissWithRightClick)
                {
                    result.Reason =
                        "the game offers no dismiss for this letter: it needs an answer, so its popup holds the clock (ui/window/accept presses its button)";
                    result.Remaining = StackLetters().Count;
                    return ApiResult<LetterDismissResultDto>.Ok(result);
                }

                // The game's own dismiss: what the stack's right-click calls.
                stack.RemoveLetter(letter);
                result.Dismissed = true;
                result.Remaining = StackLetters().Count;
                return ApiResult<LetterDismissResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<LetterDismissResultDto>.Fail(ex.Message);
            }
        }

        private static List<Letter> StackLetters()
        {
            var stack = Find.LetterStack;
            if (stack == null || stack.LettersListForReading == null)
                return new List<Letter>();
            return stack.LettersListForReading.Where(l => l != null).ToList();
        }

        private static LetterDto ToDto(Letter letter)
        {
            return new LetterDto
            {
                Id = letter.ID,
                Label = FirstLine(letter.Label),
                Text = FirstLine(LetterText(letter)),
                Def = letter.def != null ? letter.def.defName : "",
                Tick = letter.arrivalTick,
                CanDismiss = letter.CanDismissWithRightClick,
            };
        }

        // LetterText is the body the game would show when the letter is opened.
        // Letter itself declares none: StandardLetter and ChoiceLetter each
        // hold their own, so the member is looked up on the concrete type and a
        // letter without one falls back to its tooltip text.
        private static string LetterText(Letter letter)
        {
            var text = Text(Read(letter, "Text"));
            if (!string.IsNullOrEmpty(text))
                return text;
            return Text(Call(letter, "GetMouseoverText"));
        }

        // FirstLine keeps one line: a letter body is a paragraph, and the read
        // is a list of them.
        private static string FirstLine(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var line = s.Replace("\r", "").Split('\n')[0].Trim();
            if (line.Length > 300)
                line = line.Substring(0, 300) + "...";
            return line;
        }

        // Text turns a member's value into a string: a TaggedString is resolved
        // to what the game would draw, everything else via ToString.
        private static string Text(object value)
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
        // protected in Verse, and a missing name yields null instead of an
        // exception.
        private static object Read(object obj, string name)
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

        // Call runs a parameterless method found the same way; a name this game
        // build does not have yields null.
        private static object Call(object obj, string name)
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
