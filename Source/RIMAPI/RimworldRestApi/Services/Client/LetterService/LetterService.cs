using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
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
        // MaxBodyChars caps the body one letter answers with, so a long
        // narrative letter cannot fill a wait report (ticket #3491).
        private const int MaxBodyChars = 4000;

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
                    // A growth letter has no button the window read can press
                    // either: its dialog asks for a skill and a trait with its
                    // own widgets (ticket #3714), so the refusal names that
                    // instead of a popup nothing would take.
                    result.Reason = letter is ChoiceLetter_GrowthMoment
                        ? "the game offers no dismiss for this letter: it asks for a growth choice (a skill that gains a passion and a trait), which its own dialog Dialog_GrowthMomentChoices takes"
                        : "the game offers no dismiss for this letter: it needs an answer, so its popup holds the clock (ui/window/accept presses its button)";
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
                    // A growth letter is the one letter whose dialog offers no
                    // DiaOption at all: naming the window it really opens is
                    // what lets a run stop looking for an option (ticket
                    // #3714).
                    result.Reason = letter is ChoiceLetter_GrowthMoment
                        ? "ChoiceLetter_GrowthMoment is not a choice letter: it opens Dialog_GrowthMomentChoices, which asks for a skill that gains a passion and for a trait, so it has no option to press; the growth call makes that choice"
                        : $"{letter.GetType().Name} is not a choice letter, so it has no option to press";
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

                var node = ArchiveText.Read(tree, "curNode") as DiaNode;
                var options = node != null && node.options != null ? node.options : new List<DiaOption>();
                result.Options = OptionLabels(options);
                var option = MatchOption(options, request.Option);
                if (option == null)
                {
                    result.Reason = $"the dialog offers no option matching {Quoting.Quote(request.Option)}";
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
                    var next = ArchiveText.Read(rest, "curNode") as DiaNode;
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

        // ApplyGrowthChoice makes the choice a growth letter asks for (ticket
        // #3714). The letter is a ChoiceLetter_GrowthMoment, which is not a
        // ChoiceLetter: its dialog (Dialog_GrowthMomentChoices) draws skills and
        // traits with its own widgets, so there is no DiaOption to press. This
        // runs the game's own MakeChoices, the method that dialog's confirm
        // button calls, with the same arguments, and then does what that button
        // does: close the dialog and take the letter off the stack. The pawn's
        // growth points are reset by the game inside MakeChoices.
        //
        // A call without a passion asks only what the letter offers: a letter
        // that was never opened may not have generated its choices yet, and the
        // read cannot do that without acting on it.
        public ApiResult<LetterGrowthResultDto> ApplyGrowthChoice(LetterGrowthRequestDto request)
        {
            try
            {
                if (request == null)
                    return ApiResult<LetterGrowthResultDto>.Fail("the request needs the letter's id");
                var stack = Find.LetterStack;
                if (stack == null)
                    return ApiResult<LetterGrowthResultDto>.Fail("no letter stack: no colony is loaded");
                var letter = StackLetters().FirstOrDefault(l => l.ID == request.Id);
                if (letter == null)
                    return ApiResult<LetterGrowthResultDto>.Fail(
                        $"no letter with id {request.Id} stands; the letters read prints the ids");

                var result = new LetterGrowthResultDto
                {
                    Label = letter.Label,
                    Remaining = StackLetters().Count,
                };
                var growth = letter as ChoiceLetter_GrowthMoment;
                if (growth == null)
                {
                    result.Reason = $"{letter.GetType().Name} is not a growth letter, so it has no growth choice to make";
                    return ApiResult<LetterGrowthResultDto>.Ok(result);
                }

                var pawn = ArchiveText.Read(growth, "pawn") as Pawn;
                result.Pawn = pawn != null ? pawn.LabelShort : "";

                var offer = ReadGrowth(growth);
                if (offer.PassionList == null || offer.TraitList == null)
                {
                    // The game generates the choices the first time the letter is
                    // opened; a caller that got here first gets them now, without
                    // a window (TrySetChoices builds no UI).
                    ArchiveText.Call(growth, "TrySetChoices");
                    offer = ReadGrowth(growth);
                }
                result.PassionChoices = offer.Passions;
                result.TraitChoices = offer.Traits;
                result.NoTraitOption = offer.NoTrait;
                result.PassionGains = offer.Gains;

                if (ArchiveText.Read(growth, "choiceMade") is bool made && made)
                {
                    result.Reason = "the growth choice is already made: the letter only waits to leave the stack";
                    return ApiResult<LetterGrowthResultDto>.Ok(result);
                }

                var words = GrowthChoiceRules.Words(request.Passion);
                if (words.Count == 0)
                {
                    result.Reason = offer.Gains > 1
                        ? $"the letter asks for {offer.Gains} passions: name that many of the offered skills, or their indexes, in passion"
                        : "name the skill that gains the passion, or its index, in passion";
                    return ApiResult<LetterGrowthResultDto>.Ok(result);
                }
                if (offer.Gains > 0 && words.Count != offer.Gains)
                {
                    result.Reason = $"the letter asks for {offer.Gains} passion(s) and {words.Count} were named";
                    return ApiResult<LetterGrowthResultDto>.Ok(result);
                }

                var indices = new List<int>(words.Count);
                foreach (var word in words)
                {
                    var index = GrowthChoiceRules.Resolve(offer.Passions, word, out var problem);
                    if (index < 0)
                    {
                        result.Reason = problem;
                        return ApiResult<LetterGrowthResultDto>.Ok(result);
                    }
                    indices.Add(index);
                }

                var traitWord = (request.Trait ?? "").Trim();
                object chosenTrait = null;
                if (GrowthChoiceRules.IsNoTrait(traitWord))
                {
                    if (!offer.NoTrait)
                    {
                        result.Reason = "this letter offers no \"no trait\" option: name one of "
                            + GrowthChoiceRules.OfferList(offer.Traits);
                        return ApiResult<LetterGrowthResultDto>.Ok(result);
                    }
                }
                else if (traitWord.Length > 0)
                {
                    var traitIndex = GrowthChoiceRules.Resolve(offer.Traits, traitWord, out var problem);
                    if (traitIndex < 0)
                    {
                        result.Reason = problem;
                        return ApiResult<LetterGrowthResultDto>.Ok(result);
                    }
                    chosenTrait = offer.TraitList[traitIndex];
                }
                else if (offer.Traits.Count > 0 && !offer.NoTrait)
                {
                    result.Reason = "the letter asks for a trait: name one of "
                        + GrowthChoiceRules.OfferList(offer.Traits);
                    return ApiResult<LetterGrowthResultDto>.Ok(result);
                }

                var make = growth.GetType().GetMethod("MakeChoices",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (make == null)
                    return ApiResult<LetterGrowthResultDto>.Fail(
                        "ChoiceLetter_GrowthMoment.MakeChoices was not found in this game build");
                var chosen = SkillList(make.GetParameters()[0].ParameterType, offer.PassionList, indices);
                make.Invoke(growth, new[] { chosen, chosenTrait });
                result.Applied = true;

                foreach (var index in indices)
                {
                    var skill = offer.PassionList[index] as SkillDef;
                    if (skill == null || pawn == null || pawn.skills == null)
                        continue;
                    var record = pawn.skills.GetSkill(skill);
                    var label = ArchiveText.Resolve(ArchiveText.Read(skill, "LabelCap"));
                    var passion = record != null ? ArchiveText.Resolve(ArchiveText.Read(record, "passion")) : "";
                    result.ChosenPassions.Add(passion.Length > 0 ? label + " (" + passion + ")" : label);
                }
                if (chosenTrait is Trait gained)
                    result.ChosenTrait = ArchiveText.Resolve(ArchiveText.Read(gained, "LabelCap"));

                // The game's own confirm closes the dialog first and then takes
                // the letter off the stack; a dialog left open would hold the
                // clock on a letter that no longer stands.
                CloseGrowthDialog(growth);
                Find.LetterStack?.RemoveLetter(letter);
                result.Remaining = StackLetters().Count;
                LogApi.Info($"[LetterService] Growth choice for letter {letter.ID} {letter.Label}: {pawn?.LabelShort}");
                return ApiResult<LetterGrowthResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<LetterGrowthResultDto>.Fail(ex.Message);
            }
        }

        // GrowthOffer is what a growth letter offers, read without touching the
        // game's state: the skills that may gain a passion, the traits, whether
        // "no trait" is one of them and how many passions the letter asks for.
        private sealed class GrowthOffer
        {
            public IList PassionList;
            public IList TraitList;
            public List<string> Passions = new List<string>();
            public List<string> Traits = new List<string>();
            public bool NoTrait;
            public int Gains;
        }

        private static GrowthOffer ReadGrowth(ChoiceLetter_GrowthMoment growth)
        {
            var offer = new GrowthOffer
            {
                PassionList = ArchiveText.Read(growth, "passionChoices") as IList,
                TraitList = ArchiveText.Read(growth, "traitChoices") as IList,
            };
            offer.Passions = LabelsOf(offer.PassionList);
            offer.Traits = LabelsOf(offer.TraitList);
            offer.NoTrait = ArchiveText.Read(growth, "noTraitOptionShown") is bool noTrait && noTrait;
            offer.Gains = ArchiveText.Read(growth, "passionGainsCount") is int gains ? gains : 0;
            return offer;
        }

        // LabelsOf names the entries a caller picks from: the label the game
        // draws for a skill (Shooting) or a trait (Tough).
        private static List<string> LabelsOf(IList entries)
        {
            var labels = new List<string>();
            if (entries == null)
                return labels;
            foreach (var entry in entries)
                labels.Add(ArchiveText.Resolve(ArchiveText.Read(entry, "LabelCap")));
            return labels;
        }

        // SkillList builds the argument MakeChoices takes, in the element type
        // this game build declares, so a build that changed the list's type
        // costs no compile-time guess.
        private static object SkillList(Type parameterType, IList source, List<int> indices)
        {
            var listType = parameterType != null && parameterType.IsGenericType
                ? parameterType
                : typeof(List<SkillDef>);
            if (!listType.IsClass || listType.IsAbstract)
                listType = typeof(List<SkillDef>);
            var list = (IList)Activator.CreateInstance(listType);
            foreach (var index in indices)
                list.Add(source[index]);
            return list;
        }

        // CloseGrowthDialog takes this letter's own growth dialog off the stack,
        // the way the confirm button closes it. A dialog a player opened and
        // this call answered must not stay up.
        private static void CloseGrowthDialog(ChoiceLetter_GrowthMoment growth)
        {
            var windows = Find.WindowStack?.Windows;
            if (windows == null)
                return;
            foreach (var window in windows.OfType<Dialog_GrowthMomentChoices>())
            {
                if (ArchiveText.Read(window, "letter") == growth)
                {
                    Find.WindowStack.TryRemove(window, doCloseSound: false);
                    return;
                }
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
            var own = ArchiveText.Read(tree, "title") as string;
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
            return ArchiveText.Resolve(ArchiveText.Read(option, "text"));
        }

        private static bool IsDisabled(DiaOption option)
        {
            return ArchiveText.Read(option, "disabled") is bool disabled && disabled;
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
            var dto = new LetterDto
            {
                Id = letter.ID,
                Label = ArchiveText.FirstLine(letter.Label),
                Text = ArchiveText.FirstLine(ArchiveText.LetterBody(letter)),
                Body = BodyText(letter),
                Options = ChoicesOf(letter),
                Def = letter.def != null ? letter.def.defName : "",
                Tick = letter.arrivalTick,
                CanDismiss = letter.CanDismissWithRightClick,
                QuestId = QuestIdOf(letter),
                Pauses = PausesGame(letter),
            };
            // A growth letter has no DiaOption to press, so its choices travel
            // in their own fields: the skill that gains a passion, the traits
            // and the letter's own "no trait" option (ticket #3714).
            var growth = letter as ChoiceLetter_GrowthMoment;
            if (growth != null)
            {
                var offer = ReadGrowth(growth);
                dto.PassionChoices = offer.Passions;
                dto.TraitChoices = offer.Traits;
                dto.NoTraitOption = offer.NoTrait;
                dto.PassionGains = offer.Gains;
            }
            return dto;
        }

        // BodyText is the whole body, not the first line: the wait report
        // hands a run the letter it arrived on, and a first line of a quest
        // offer says nothing about the reward (ticket #3491). A cap keeps a
        // long narrative letter from dominating an answer.
        private static string BodyText(Letter letter)
        {
            var body = ArchiveText.LetterBody(letter);
            if (string.IsNullOrEmpty(body))
                return "";
            body = body.Replace("\r", "").Trim();
            if (body.Length > MaxBodyChars)
                body = body.Substring(0, MaxBodyChars) + "...";
            return body;
        }

        // ChoicesOf is the options the letter's dialog would draw, read from
        // the letter itself: opening a letter to look at it can throw the
        // offer away (NewQuestLetter switches to the Quests tab), so this must
        // not open anything (ticket #3491, see AnswerLetter).
        private static List<string> ChoicesOf(Letter letter)
        {
            var choice = letter as ChoiceLetter;
            if (choice == null || choice.Choices == null)
                return new List<string>();
            return OptionLabels(choice.Choices.Where(o => o != null).ToList());
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

    }
}
