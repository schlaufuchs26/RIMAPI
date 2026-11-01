using System;
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
    public class WindowService : IWindowService
    {
        public ApiResult<List<OpenWindowDto>> ListWindows()
        {
            try
            {
                var windows = Find.WindowStack?.Windows;
                var list = new List<OpenWindowDto>();
                if (windows != null)
                {
                    foreach (var w in windows)
                    {
                        list.Add(new OpenWindowDto
                        {
                            WindowType = w.GetType().Name,
                            ForcePause = w.forcePause,
                            Title = WindowTitle(w),
                            Text = WindowText(w),
                            Buttons = WindowButtons(w),
                            Name = WindowName(w),
                            SecondName = WindowSecondName(w),
                        });
                    }
                }
                return ApiResult<List<OpenWindowDto>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ApiResult<List<OpenWindowDto>>.Fail(ex.Message);
            }
        }

        public ApiResult<WindowAcceptResultDto> AcceptWindows(WindowAcceptRequestDto request)
        {
            try
            {
                request = request ?? new WindowAcceptRequestDto();
                var result = new WindowAcceptResultDto();
                var windows = Find.WindowStack?.Windows;
                if (windows == null)
                    return ApiResult<WindowAcceptResultDto>.Ok(result);

                bool byType = request.WindowTypes != null && request.WindowTypes.Count > 0;
                Window target = null;
                // Topmost first: the dialog a player would have to answer is the
                // one on top, not the one lowest in the stack.
                for (int i = windows.Count - 1; i >= 0 && target == null; i--)
                {
                    var w = windows[i];
                    if (w == null)
                        continue;
                    if (byType)
                    {
                        var typeName = w.GetType().Name;
                        if (request.WindowTypes.Any(t =>
                                !string.IsNullOrEmpty(t) &&
                                typeName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
                            target = w;
                    }
                    else if (w.forcePause)
                    {
                        target = w;
                    }
                }

                if (target == null)
                    return ApiResult<WindowAcceptResultDto>.Ok(result);

                string button;
                string reason;
                List<string> names;
                if (TryPressAccept(target, request, out button, out names, out reason))
                {
                    result.AcceptedCount = 1;
                    result.AcceptedWindows.Add(target.GetType().Name);
                    result.Buttons.Add(button);
                    result.Names.AddRange(names);
                    LogApi.Info($"[WindowService] Pressed {button} on {target.GetType().Name}");
                }
                else
                {
                    result.StillOpen.Add(target.GetType().Name);
                    result.Reason = reason;
                }

                return ApiResult<WindowAcceptResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<WindowAcceptResultDto>.Fail(ex.Message);
            }
        }

        // TryPressAccept runs the code the game's own button runs, so the
        // dialog's action (naming the faction, choosing the option) happens
        // instead of the window being pulled off the stack. A window type the
        // bridge does not know reports why instead of pretending to click.
        private static bool TryPressAccept(Window window, WindowAcceptRequestDto request,
            out string button, out List<string> names, out string reason)
        {
            button = "";
            reason = null;
            names = new List<string>();

            var box = window as Dialog_MessageBox;
            if (box != null)
            {
                float wait = InteractionWait(box);
                if (wait > 0f)
                {
                    reason = $"the dialog refuses input for another {wait:0.0} s";
                    return false;
                }
                button = string.IsNullOrEmpty(box.buttonAText) ? "OK" : box.buttonAText;
                if (box.buttonAAction != null)
                    box.buttonAAction();
                box.Close();
                return true;
            }

            var tree = window as Dialog_NodeTree;
            if (tree != null)
            {
                var node = FindField(tree, "curNode")?.GetValue(tree) as DiaNode;
                var option = node?.options?.FirstOrDefault(o => o != null && !o.disabled);
                if (option == null)
                {
                    reason = "the dialog offers no option that can be pressed";
                    return false;
                }
                var activate = option.GetType().GetMethod("Activate",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (activate == null)
                {
                    reason = "DiaOption.Activate was not found in this game build";
                    return false;
                }
                button = ReadString(option, "text");
                activate.Invoke(option, null);
                return true;
            }

            // The naming dialogs (faction and settlement, settlement alone, a
            // gravship) are no message boxes: the game draws their "OK" as an
            // inline button whose code validates the typed name, calls Named /
            // NamedSecond, shows the gained-name message and removes the window
            // (Dialog_GiveName.DoWindowContents). The text fields write into
            // curName / curSecondName, so the same steps run from here with the
            // names this call asked for, else the ones the dialog already
            // holds; which is the generated one until someone types another.
            // Without this the dialog held the clock and only a screen click
            // could answer it (ticket #3138), and the generated name was the
            // only name a colony ever got (fox ticket #3929).
            var giveName = window as Dialog_GiveName;
            if (giveName != null)
            {
                bool useSecond = ReadBool(giveName, "useSecondName");
                string problem;
                List<string> plan = NameDialogRules.Plan(
                    useSecond,
                    request == null ? null : request.Name,
                    request == null ? null : request.SecondName,
                    ReadString(giveName, "curName"),
                    ReadString(giveName, "curSecondName"),
                    out problem);
                if (problem != null)
                {
                    reason = problem;
                    return false;
                }
                // The fields are set before the validation and Named run, so
                // the dialog names the colony with exactly what this call
                // asked for, and a read after the press sees the same.
                SetField(giveName, "curName", plan[0]);
                if (useSecond)
                    SetField(giveName, "curSecondName", plan[1]);
                if (!InvokeBool(giveName, "IsValidName", plan[0]))
                {
                    reason = NameDialogRules.Refusal(
                        NameDialogRules.FieldLabel(false, useSecond), plan[0],
                        InvalidNameMessage(giveName, false));
                    return false;
                }
                if (useSecond && !InvokeBool(giveName, "IsValidSecondName", plan[1]))
                {
                    reason = NameDialogRules.Refusal(
                        NameDialogRules.FieldLabel(true, true), plan[1],
                        InvalidNameMessage(giveName, true));
                    return false;
                }
                Invoke(giveName, "Named", plan[0]);
                if (useSecond)
                    Invoke(giveName, "NamedSecond", plan[1]);
                names.AddRange(plan);
                var gained = ReadString(giveName, "gainedNameMessageKey");
                if (!string.IsNullOrEmpty(gained))
                {
                    var text = useSecond ? gained.Translate(plan[0], plan[1]) : gained.Translate(plan[0]);
                    Messages.Message(text, MessageTypeDefOf.TaskCompletion, false);
                }
                button = "OK";
                Find.WindowStack.TryRemove(giveName, true);
                return true;
            }

            reason = "the bridge knows no accept button for this window type";
            return false;
        }

        // InteractionWait is how long a message box still ignores clicks: the
        // game disables its buttons for interactionDelay seconds after it opens,
        // and pressing earlier would answer a dialog a player could not.
        private static float InteractionWait(Dialog_MessageBox box)
        {
            if (box.interactionDelay <= 0f)
                return 0f;
            var created = FindField(box, "creationRealTime");
            if (created == null)
                return 0f;
            var start = (float)created.GetValue(box);
            if (start < 0f)
                return 0f;
            return Math.Max(0f, box.interactionDelay - (UnityEngine.Time.realtimeSinceStartup - start));
        }

        private static string WindowTitle(Window w)
        {
            var box = w as Dialog_MessageBox;
            if (box != null)
                return box.title ?? "";
            return ReadString(w, "title");
        }

        private static string WindowText(Window w)
        {
            var box = w as Dialog_MessageBox;
            if (box != null)
                return box.text.Resolve();
            var tree = w as Dialog_NodeTree;
            if (tree != null)
            {
                var node = FindField(tree, "curNode")?.GetValue(tree) as DiaNode;
                return node != null ? node.text.Resolve() : "";
            }
            var giveName = w as Dialog_GiveName;
            if (giveName != null)
            {
                // The naming dialogs show the game's own question ("... thinks
                // that you should give your faction a name"), and that text is
                // what tells a caller which names the box wants (fox ticket
                // #3929). Both halves are read for the pair dialog.
                string text = DialogMessage(giveName, false);
                if (ReadBool(giveName, "useSecondName"))
                {
                    string second = DialogMessage(giveName, true);
                    if (!string.IsNullOrEmpty(second))
                        text = string.IsNullOrEmpty(text) ? second : text + " " + second;
                }
                return text;
            }
            return "";
        }

        // DialogMessage translates the game's own message key of a naming
        // dialog, with the pawn the dialog suggests the name for.
        private static string DialogMessage(Dialog_GiveName dialog, bool second)
        {
            string key = ReadString(dialog, second ? "secondNameMessageKey" : "nameMessageKey");
            if (string.IsNullOrEmpty(key))
                return "";
            var pawn = FindField(dialog, "suggestingPawn")?.GetValue(dialog) as Pawn;
            if (pawn != null)
                return key.Translate(pawn.LabelShort, pawn).Resolve();
            return key.Translate().Resolve();
        }

        // WindowName is the name a naming dialog holds in its first field: the
        // faction for the pair dialog, the only name for the others.
        private static string WindowName(Window w)
        {
            var giveName = w as Dialog_GiveName;
            if (giveName == null)
                return "";
            return ReadString(giveName, "curName").Trim();
        }

        // WindowSecondName is the settlement a pair dialog holds; empty on a
        // dialog that asks one name.
        private static string WindowSecondName(Window w)
        {
            var giveName = w as Dialog_GiveName;
            if (giveName == null || !ReadBool(giveName, "useSecondName"))
                return "";
            return ReadString(giveName, "curSecondName").Trim();
        }

        // InvalidNameMessage is the game's own reason for a rejected name, so
        // a refusal repeats what the dialog itself would have shown.
        private static string InvalidNameMessage(Dialog_GiveName dialog, bool second)
        {
            string key = ReadString(dialog, second ? "invalidSecondNameMessageKey" : "invalidNameMessageKey");
            if (string.IsNullOrEmpty(key) || !key.CanTranslate())
                return "";
            return key.Translate().Resolve();
        }

        private static List<string> WindowButtons(Window w)
        {
            var labels = new List<string>();
            var box = w as Dialog_MessageBox;
            if (box != null)
            {
                if (!string.IsNullOrEmpty(box.buttonAText)) labels.Add(box.buttonAText);
                if (!string.IsNullOrEmpty(box.buttonBText)) labels.Add(box.buttonBText);
                if (!string.IsNullOrEmpty(box.buttonCText)) labels.Add(box.buttonCText);
                return labels;
            }
            var giveName = w as Dialog_GiveName;
            if (giveName != null)
            {
                // The naming dialogs draw their OK inline, so no field holds
                // the label; the button the bridge presses is that OK.
                labels.Add("OK");
                return labels;
            }
            var tree = w as Dialog_NodeTree;
            if (tree != null)
            {
                var node = FindField(tree, "curNode")?.GetValue(tree) as DiaNode;
                if (node?.options != null)
                {
                    foreach (var option in node.options)
                    {
                        if (option == null)
                            continue;
                        labels.Add(ReadString(option, "text"));
                    }
                }
            }
            return labels;
        }

        // The fields these reads need are private or protected in Verse, so
        // they are looked up by name up the type hierarchy; a missing name
        // yields an empty value instead of an exception.
        private static FieldInfo FindField(object obj, string name)
        {
            for (var type = obj.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            return null;
        }

        private static string ReadString(object obj, string name)
        {
            var value = FindField(obj, name)?.GetValue(obj);
            return value != null ? value.ToString() : "";
        }

        private static bool ReadBool(object obj, string name)
        {
            var value = FindField(obj, name)?.GetValue(obj);
            return value is bool && (bool)value;
        }

        // SetField writes a field the game reads, which is how a wanted name
        // reaches the dialog's own OK (fox ticket #3929). A missing field is
        // an error, not a silent no-op: the bridge would be running a game
        // build it does not know.
        private static void SetField(object obj, string name, string value)
        {
            var field = FindField(obj, name);
            if (field == null)
                throw new MissingFieldException(obj.GetType().Name, name);
            field.SetValue(obj, value);
        }

        // Dialog_GiveName's validation and naming methods are family (protected)
        // overrides on the concrete dialog class, so they are looked up the same
        // way the fields are and called exactly as the game's own OK button
        // calls them. A missing method is an error, not a "no button": the
        // bridge would be running a different game build than it knows.
        private static bool InvokeBool(object obj, string name, string argument)
        {
            var value = Invoke(obj, name, argument);
            return value is bool && (bool)value;
        }

        private static object Invoke(object obj, string name, string argument)
        {
            var method = FindMethod(obj.GetType(), name, 1);
            if (method == null)
                throw new MissingMethodException(obj.GetType().Name, name);
            return method.Invoke(obj, new object[] { argument });
        }

        private static MethodInfo FindMethod(Type type, string name, int parameterCount)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var method = t
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == parameterCount);
                if (method != null)
                    return method;
            }
            return null;
        }

        public ApiResult<WindowCloseResultDto> CloseWindows(WindowCloseRequestDto request)
        {
            try
            {
                request = request ?? new WindowCloseRequestDto();
                var windowStack = Find.WindowStack;
                var result = new WindowCloseResultDto();
                if (windowStack?.Windows == null)
                    return ApiResult<WindowCloseResultDto>.Ok(result);

                bool byType = request.WindowTypes != null && request.WindowTypes.Count > 0;

                // Snapshot first — removing mutates the live collection.
                var toClose = windowStack.Windows
                    .Where(w =>
                    {
                        var typeName = w.GetType().Name;
                        if (byType)
                        {
                            return request.WindowTypes.Any(t =>
                                !string.IsNullOrEmpty(t) &&
                                typeName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
                        }
                        // No explicit types: close force-pause windows (the
                        // unattended-benchmark nuisance: colony-name dialog,
                        // debug log on error, etc.) when ForcePauseOnly is set.
                        return !request.ForcePauseOnly || w.forcePause;
                    })
                    .ToList();

                foreach (var w in toClose)
                {
                    if (windowStack.TryRemove(w, doCloseSound: false))
                    {
                        result.ClosedCount++;
                        result.ClosedWindows.Add(w.GetType().Name);
                    }
                }

                if (result.ClosedCount > 0)
                {
                    LogApi.Info($"[WindowService] Closed {result.ClosedCount} window(s): "
                        + string.Join(", ", result.ClosedWindows));
                }

                return ApiResult<WindowCloseResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<WindowCloseResultDto>.Fail(ex.Message);
            }
        }

        public ApiResult ShowMessage(WindowMessageRequestDto request)
        {
            try
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    // Create a simple Node with one "OK" option
                    DiaNode node = new DiaNode(request.Text);
                    DiaOption option = new DiaOption(request.ButtonText)
                    {
                        resolveTree = true // Closes the dialog
                    };
                    node.options.Add(option);

                    // Create the Window
                    Dialog_NodeTree window = new Dialog_NodeTree(node, delayInteractivity: false);
                    if (!string.IsNullOrEmpty(request.Title))
                    {
                        // Some versions of RimWorld don't show title on NodeTree, 
                        // but DiaNode doesn't hold title directly usually. 
                        // We can inject it into the text or use a Letter if preferred.
                        // Standard Dialog_NodeTree doesn't always support a top header title 
                        // explicitly distinct from text, but let's try mostly standard usage.
                    }

                    Find.WindowStack.Add(window);
                });

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult ShowDialog(WindowDialogRequestDto request)
        {
            try
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    DiaNode node = new DiaNode(request.Text);

                    if (request.Options != null)
                    {
                        foreach (var optDto in request.Options)
                        {
                            DiaOption option = new DiaOption(optDto.Label);

                            // Handle closing logic
                            option.resolveTree = optDto.ResolveTree;

                            // Action Logic
                            if (!string.IsNullOrEmpty(optDto.ActionId))
                            {
                                option.action = () =>
                                {
                                    // Log the choice to console (or you could send a callback webhook here)
                                    LogApi.Info($"[WindowService] User selected option: {optDto.Label} (ID: {optDto.ActionId})");
                                };
                            }

                            node.options.Add(option);
                        }
                    }

                    // If no options provided, add a default Close
                    if (node.options.Count == 0)
                    {
                        node.options.Add(new DiaOption("Close") { resolveTree = true });
                    }

                    Dialog_NodeTree window = new Dialog_NodeTree(node, delayInteractivity: false);
                    Find.WindowStack.Add(window);
                });

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }
    }
}