using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // Simple "OK" Message
    public class WindowMessageRequestDto
    {
        [ParameterDescription("The title the message window shows.")]
        public string Title { get; set; } = "Alert";
        [ParameterDescription("The text the message window shows.")]
        public string Text { get; set; }
        [ParameterDescription("The text on the one button.")]
        public string ButtonText { get; set; } = "OK";
    }

    // Dialog with Choices
    public class WindowDialogRequestDto
    {
        [ParameterDescription("The heading the dialog draws across its top.")]
        public string Title { get; set; }
        [ParameterDescription("The text the dialog shows above its options.")]
        public string Text { get; set; }
        [ParameterDescription("The options the dialog offers, one button each.")]
        public List<DialogOptionDto> Options { get; set; }
    }

    public class DialogOptionDto
    {
        public string Label { get; set; }

        // Optional: Perform an action when clicked?
        // For a basic implementation, we might just log the click or close the window.
        // Advanced: You could map this to a callback ID if needed.
        [ParameterDescription("Optional: Perform an action when clicked?")]
        public string ActionId { get; set; }
        [ParameterDescription("Whether choosing it closes the window.")]
        public bool ResolveTree { get; set; } = true; // Close window after click
    }

    // Close one or more open windows by type name.
    // WindowTypes is matched case-insensitively against each open window's
    // runtime type name (e.g. "Dialog_NamePlayerSettlement", "EditWindow_Log");
    // a substring is enough so "Log" closes EditWindow_Log. When omitted/empty,
    // ForcePauseOnly (default true) closes every window that force-pauses the
    // game — the unattended-benchmark nuisance case.
    public class WindowCloseRequestDto
    {
        [ParameterDescription("The window kinds to close.")]
        public List<string> WindowTypes { get; set; }
        [ParameterDescription("Whether only a force-pause window is closed.")]
        public bool ForcePauseOnly { get; set; } = true;
    }

    public class WindowCloseResultDto
    {
        [ParameterDescription("How many windows the call closed.")]
        public int ClosedCount { get; set; }
        [ParameterDescription("The windows the call closed, one entry each.")]
        public List<string> ClosedWindows { get; set; } = new List<string>();
    }

    // One open window as the caller sees it. ForcePause marks the windows that
    // hold the game's clock: while one is open the colony does not tick,
    // whatever speed it was told. Title, Text and Buttons carry what the game
    // shows, so a caller can tell a modal from a panel and name the button it
    // would have clicked.
    public class OpenWindowDto
    {
        [ParameterDescription("The kind of window that is open.")]
        public string WindowType { get; set; }
        [ParameterDescription("Whether the window forces a pause.")]
        public bool ForcePause { get; set; }
        [ParameterDescription("The title the open window shows.")]
        public string Title { get; set; }
        [ParameterDescription("The text the open window shows.")]
        public string Text { get; set; }
        [ParameterDescription("The buttons the open window offers.")]
        public List<string> Buttons { get; set; } = new List<string>();
        // The names a naming dialog (Dialog_GiveName and its faction,
        // settlement and gravship subclasses) holds right now, fox ticket
        // #3929. Name is the faction (or the only name the dialog asks for),
        // SecondName the settlement. Empty on every other window, and on a
        // naming dialog whose field the game left empty.
        public string Name { get; set; }
        [ParameterDescription("The window's second name field.")]
        public string SecondName { get; set; }
    }

    // Press the accept button of one open window. WindowTypes is matched
    // case-insensitively as a substring of each window's type name; with none
    // the topmost window that force-pauses the game is the one pressed.
    public class WindowAcceptRequestDto
    {
        [ParameterDescription("The window kinds to accept.")]
        public List<string> WindowTypes { get; set; }

        // The names a naming dialog should be given before its OK runs (fox
        // ticket #3929). Empty keeps the name the dialog already holds, which
        // is the game's generated one. Name is the faction (or the only name
        // the dialog asks for), SecondName the settlement.
        public string Name { get; set; }
        [ParameterDescription("The name to type into the second field.")]
        public string SecondName { get; set; }
    }

    public class WindowAcceptResultDto
    {
        [ParameterDescription("How many windows the call accepted.")]
        public int AcceptedCount { get; set; }
        [ParameterDescription("The windows the call accepted, one entry each.")]
        public List<string> AcceptedWindows { get; set; } = new List<string>();
        // The label of the button each accepted window got, in the same order.
        [ParameterDescription("The label of the button each accepted window got, in the same order.")]
        public List<string> Buttons { get; set; } = new List<string>();
        // The names a naming dialog was given, in the order the dialog asks
        // them, so the caller can compare them with what the game holds.
        [ParameterDescription("The names a naming dialog was given, in the order the dialog asks them, so the caller can compare them with what the game holds.")]
        public List<string> Names { get; set; } = new List<string>();
        // Windows that matched but could not be pressed; Reason says why.
        [ParameterDescription("Windows that matched but could not be pressed; Reason says why.")]
        public List<string> StillOpen { get; set; } = new List<string>();
        [ParameterDescription("Why it accepted nothing, when it did not.")]
        public string Reason { get; set; }
    }
}