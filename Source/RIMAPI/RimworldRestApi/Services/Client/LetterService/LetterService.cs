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
        // A caller asks for the newest few; the caps keep a wide `since` from
        // answering the whole colony history over the bridge.
        private const int MaxArchivedLetters = 200;
        private const int DefaultArchivedLetters = 25;

        public ApiResult<List<LetterDto>> ListLetters(bool markSeen)
        {
            try
            {
                var standing = StackLetters();
                // The game keeps no read flag, so the bridge remembers what it
                // has reported: this call answers "new" for the letters no
                // earlier read showed. A reporting read marks the whole stack
                // seen; a peek (markSeen false) leaves it for the `letters`
                // read, because the digest and the wait look at the stack far
                // more often than a run asks what is new (tickets #3156, #3214).
                var ids = standing.Select(l => l.ID);
                var fresh = markSeen ? LetterSeen.Report(ids) : LetterSeen.Peek(ids);
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

        // ListArchivedLetters answers the letters the game received at or after
        // since, standing or not. The stack drops a letter whose target is gone
        // (LetterStackUpdate) while the pause it caused stays, so a pause with no
        // window and no letter on the stack is only nameable from the archive
        // (ticket #3357). The archive is the list the save writes under
        // <history><archive>, so this is what a save file would show, without a
        // save.
        public ApiResult<List<LetterDto>> ListArchivedLetters(int since, int limit)
        {
            try
            {
                if (limit <= 0)
                    limit = DefaultArchivedLetters;
                if (limit > MaxArchivedLetters)
                    limit = MaxArchivedLetters;
                var archive = Find.Archive;
                if (archive == null)
                    return ApiResult<List<LetterDto>>.Fail("no archive: no colony is loaded");

                var archived = new List<Letter>();
                foreach (var archivable in archive.ArchivablesListForReading)
                {
                    var letter = archivable as Letter;
                    if (letter == null)
                        continue;
                    if (since > 0 && letter.arrivalTick < since)
                        continue;
                    archived.Add(letter);
                }
                archived.Sort((a, b) => a.arrivalTick.CompareTo(b.arrivalTick));
                if (archived.Count > limit)
                    archived = archived.Skip(archived.Count - limit).ToList();

                var result = new List<LetterDto>(archived.Count);
                foreach (var letter in archived)
                {
                    var dto = ToDto(letter);
                    // An archived letter is not on the stack, so no reporting
                    // read can have shown it; the archive branch never marks
                    // anything seen.
                    dto.New = false;
                    result.Add(dto);
                }
                return ApiResult<List<LetterDto>>.Ok(result);
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

        // AnswerLetter opens a choice letter's dialog and presses the option the
        // caller names, the same code the game's own button runs, so the
        // letter's action happens (a joiner joins, a quest is taken) instead of
        // the window being pulled off the stack (ticket #3188). A letter that
        // offers no dialog is refused with its type name, and an option that
        // matches none closes the dialog again and names the options, so a
        // wrong word costs one call and no stuck popup.
        public ApiResult<LetterAnswerResultDto> AnswerLetter(LetterAnswerRequestDto request)
        {
            try
            {
                if (request == null)
                    return ApiResult<LetterAnswerResultDto>.Fail(
                        "the request needs the letter's id and the option to press");
                var stack = Find.LetterStack;
                if (stack == null)
                    return ApiResult<LetterAnswerResultDto>.Fail("no letter stack: no colony is loaded");
                var letter = StackLetters().FirstOrDefault(l => l.ID == request.Id);
                if (letter == null)
                    return ApiResult<LetterAnswerResultDto>.Fail(
                        $"no letter with id {request.Id} stands; the letters read prints the ids");

                var result = new LetterAnswerResultDto { Label = letter.Label };
                var choice = letter as ChoiceLetter;
                if (choice == null)
                {
                    result.Reason =
                        $"{letter.GetType().Name} is not a choice letter, so it has no option to press";
                    result.Remaining = StackLetters().Count;
                    return ApiResult<LetterAnswerResultDto>.Ok(result);
                }

                // The choices are read from the letter, not from a window it
                // opens: NewQuestLetter.OpenLetter switches to the Quests tab
                // and removes the letter, so opening a letter to look at it can
                // throw the offer away (ticket #3188). ChoiceLetter.OpenLetter
                // builds exactly this dialog from the same Choices.
                var offered = choice.Choices != null ? choice.Choices.Where(o => o != null).ToList() : new List<DiaOption>();
                if (offered.Count == 0)
                {
                    result.Reason = $"{letter.GetType().Name} offers no choice to press";
                    result.Remaining = StackLetters().Count;
                    return ApiResult<LetterAnswerResultDto>.Ok(result);
                }

                bool openedHere;
                var tree = ChoiceDialog(letter, choice, offered, out openedHere);
                if (tree == null)
                {
                    result.Reason = "no dialog could be put on the stack, so there is no option to press";
                    result.Remaining = StackLetters().Count;
                    return ApiResult<LetterAnswerResultDto>.Ok(result);
                }

                var node = Read(tree, "curNode") as DiaNode;
                var options = node != null && node.options != null ? node.options : new List<DiaOption>();
                result.Options = OptionLabels(options);
                var option = MatchOption(options, request.Option);
                if (option == null)
                {
                    result.Reason = $"the dialog offers no option matching {Quote(request.Option)}";
                    result.Remaining = StackLetters().Count;
                    // The dialog was put there by this call, so it goes away
                    // again: a wrong word must not leave the clock held.
                    if (openedHere)
                        Find.WindowStack?.TryRemove(tree, doCloseSound: false);
                    return ApiResult<LetterAnswerResultDto>.Ok(result);
                }

                var activate = option.GetType().GetMethod("Activate",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (activate == null)
                    return ApiResult<LetterAnswerResultDto>.Fail(
                        "DiaOption.Activate was not found in this game build");

                result.Pressed = OptionText(option);
                activate.Invoke(option, null);
                result.Answered = true;
                LogApi.Info($"[LetterService] Answered letter {letter.ID} {letter.Label}: {result.Pressed}");

                // The read after the press is the proof: a choice that leads to
                // a second node keeps its dialog open, and a joiner's letter
                // leaves the stack.
                var rest = OpenDialogOf(tree);
                if (rest != null)
                {
                    result.StillOpen = true;
                    result.OpenWindow = rest.GetType().Name;
                    var next = Read(rest, "curNode") as DiaNode;
                    result.Options = OptionLabels(next != null ? next.options : null);
                }
                result.Remaining = StackLetters().Count;
                return ApiResult<LetterAnswerResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<LetterAnswerResultDto>.Fail(ex.Message);
            }
        }

        // ChoiceDialog returns the dialog to press in: one this letter already
        // has on the stack, or a new one built from the letter's own choices,
        // which is the dialog ChoiceLetter.OpenLetter would show. openedHere
        // says whether this call put it there, so a wrong word can take it away
        // again without closing a window the player opened.
        private static Dialog_NodeTree ChoiceDialog(Letter letter, ChoiceLetter choice,
            List<DiaOption> offered, out bool openedHere)
        {
            openedHere = false;
            var windows = Find.WindowStack?.Windows;
            if (windows == null)
                return null;
            foreach (var w in windows.OfType<Dialog_NodeTree>())
            {
                if (TitledFor(w, letter))
                    return w;
            }
            var node = new DiaNode(choice.Text);
            node.options.AddRange(offered);
            var tree = new Dialog_NodeTree(node, delayInteractivity: false,
                radioMode: choice.radioMode, title: choice.title);
            Find.WindowStack.Add(tree);
            openedHere = true;
            return tree;
        }

        // TitledFor says whether a dialog is this letter's: a choice letter
        // titles its window with its own label (Dialog_NodeTree's protected
        // title field, which the faction-info subclass fills).
        private static bool TitledFor(Dialog_NodeTree tree, Letter letter)
        {
            var own = Read(tree, "title") as string;
            var label = letter.Label.ToString();
            if (string.IsNullOrEmpty(own) || string.IsNullOrEmpty(label))
                return false;
            return own.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0
                || label.IndexOf(own, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // OpenDialogOf returns the window if it is still on the stack.
        private static Dialog_NodeTree OpenDialogOf(Dialog_NodeTree tree)
        {
            var windows = Find.WindowStack?.Windows;
            if (windows == null)
                return null;
            return windows.OfType<Dialog_NodeTree>().FirstOrDefault(w => w == tree);
        }

        // MatchOption finds the option a caller named: an exact label first,
        // then a label that contains the word, and never a disabled one. The
        // labels are translated, so the usual words are tried against the
        // game's own translation keys as well ("decline" has to reach "Reject").
        private static DiaOption MatchOption(List<DiaOption> options, string wanted)
        {
            if (options == null || options.Count == 0)
                return null;
            foreach (var alias in Aliases(wanted))
            {
                var exact = options.FirstOrDefault(o =>
                    o != null && !IsDisabled(o) &&
                    string.Equals(OptionText(o).Trim(), alias, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }
            foreach (var alias in Aliases(wanted))
            {
                var partial = options.FirstOrDefault(o =>
                    o != null && !IsDisabled(o) &&
                    OptionText(o).IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0);
                if (partial != null)
                    return partial;
            }
            return null;
        }

        // Aliases is the word the caller gave plus the label the game draws for
        // the usual choices.
        private static List<string> Aliases(string option)
        {
            var word = (option ?? "").Trim();
            var list = new List<string>();
            if (word.Length == 0)
                return list;
            list.Add(word);
            switch (word.ToLowerInvariant())
            {
                case "accept":
                    list.Add("AcceptButton".Translate());
                    break;
                case "reject":
                case "decline":
                    list.Add("RejectLetter".Translate());
                    break;
                case "postpone":
                case "later":
                    list.Add("PostponeLetter".Translate());
                    break;
                case "close":
                    list.Add("Close".Translate());
                    break;
            }
            return list;
        }

        private static List<string> OptionLabels(List<DiaOption> options)
        {
            var labels = new List<string>();
            if (options == null)
                return labels;
            foreach (var option in options)
            {
                if (option == null)
                    continue;
                var label = OptionText(option);
                if (IsDisabled(option))
                    label += " (disabled)";
                labels.Add(label);
            }
            return labels;
        }

        private static string OptionText(DiaOption option)
        {
            return Text(Read(option, "text"));
        }

        private static bool IsDisabled(DiaOption option)
        {
            return Read(option, "disabled") is bool disabled && disabled;
        }

        private static string Quote(string s)
        {
            return "\"" + (s ?? "") + "\"";
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
                QuestId = QuestIdOf(letter),
                Pauses = PausesGame(letter),
            };
        }

        // PausesGame is the game's own auto-pause rule for a letter, the test
        // LetterStack.ReceiveLetter runs before it stops the clock: the player's
        // setting is at or above the letter's own pause mode. A letter whose def
        // pauses on nothing never stops the clock, so it is never blamed for a
        // pause (ticket #3357).
        private static bool PausesGame(Letter letter)
        {
            if (letter?.def == null)
                return false;
            return (int)Prefs.AutomaticPauseMode >= (int)letter.def.pauseMode;
        }

        // QuestIdOf names the quest a letter announces: a quest offer is a
        // NewQuestLetter and carries the quest, so the letters read points at
        // the quests read instead of leaving the offer without an id (ticket
        // #3216). Every other letter answers null.
        private static int? QuestIdOf(Letter letter)
        {
            var choice = letter as ChoiceLetter;
            if (choice == null || choice.quest == null)
                return null;
            return choice.quest.id;
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
