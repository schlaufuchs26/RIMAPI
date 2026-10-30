using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RIMAPI.Core;
using RIMAPI.Models;
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
                if (TryPressAccept(target, out button, out reason))
                {
                    result.AcceptedCount = 1;
                    result.AcceptedWindows.Add(target.GetType().Name);
                    result.Buttons.Add(button);
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
        private static bool TryPressAccept(Window window, out string button, out string reason)
        {
            button = "";
            reason = null;

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

            reason = "the bridge knows no accept button for this window type; a screen click is the way out";
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
            return "";
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