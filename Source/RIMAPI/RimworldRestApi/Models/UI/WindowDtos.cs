using System.Collections.Generic;

namespace RIMAPI.Models
{
    // Simple "OK" Message
    public class WindowMessageRequestDto
    {
        public string Title { get; set; } = "Alert";
        public string Text { get; set; }
        public string ButtonText { get; set; } = "OK";
    }

    // Dialog with Choices
    public class WindowDialogRequestDto
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public List<DialogOptionDto> Options { get; set; }
    }

    public class DialogOptionDto
    {
        public string Label { get; set; }

        // Optional: Perform an action when clicked?
        // For a basic implementation, we might just log the click or close the window.
        // Advanced: You could map this to a callback ID if needed.
        public string ActionId { get; set; }
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
        public List<string> WindowTypes { get; set; }
        public bool ForcePauseOnly { get; set; } = true;
    }

    public class WindowCloseResultDto
    {
        public int ClosedCount { get; set; }
        public List<string> ClosedWindows { get; set; } = new List<string>();
    }

    // One open window as the caller sees it. ForcePause marks the windows that
    // hold the game's clock: while one is open the colony does not tick,
    // whatever speed it was told. Title, Text and Buttons carry what the game
    // shows, so a caller can tell a modal from a panel and name the button it
    // would have clicked.
    public class OpenWindowDto
    {
        public string WindowType { get; set; }
        public bool ForcePause { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public List<string> Buttons { get; set; } = new List<string>();
        // The names a naming dialog (Dialog_GiveName and its faction,
        // settlement and gravship subclasses) holds right now, fox ticket
        // #3929. Name is the faction (or the only name the dialog asks for),
        // SecondName the settlement. Empty on every other window, and on a
        // naming dialog whose field the game left empty.
        public string Name { get; set; }
        public string SecondName { get; set; }
    }

    // Press the accept button of one open window. WindowTypes is matched
    // case-insensitively as a substring of each window's type name; with none
    // the topmost window that force-pauses the game is the one pressed.
    public class WindowAcceptRequestDto
    {
        public List<string> WindowTypes { get; set; }

        // The names a naming dialog should be given before its OK runs (fox
        // ticket #3929). Empty keeps the name the dialog already holds, which
        // is the game's generated one. Name is the faction (or the only name
        // the dialog asks for), SecondName the settlement.
        public string Name { get; set; }
        public string SecondName { get; set; }
    }

    public class WindowAcceptResultDto
    {
        public int AcceptedCount { get; set; }
        public List<string> AcceptedWindows { get; set; } = new List<string>();
        // The label of the button each accepted window got, in the same order.
        public List<string> Buttons { get; set; } = new List<string>();
        // The names a naming dialog was given, in the order the dialog asks
        // them, so the caller can compare them with what the game holds.
        public List<string> Names { get; set; } = new List<string>();
        // Windows that matched but could not be pressed; Reason says why.
        public List<string> StillOpen { get; set; } = new List<string>();
        public string Reason { get; set; }
    }
}