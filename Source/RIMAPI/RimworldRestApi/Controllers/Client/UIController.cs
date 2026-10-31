using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Services.Interfaces;
using RIMAPI.Services;
using RIMAPI.Models;

namespace RIMAPI.BaseControllers
{
    public class UIController
    {
        private readonly IUIService _uiService;
        private readonly IWindowService _windowService;
        private readonly ILetterService _letterService;

        public UIController(IUIService uiService, IWindowService windowService, ILetterService letterService)
        {
            _uiService = uiService;
            _windowService = windowService;
            _letterService = letterService;
        }

        [Get("/api/v1/ui/alerts")]
        [EndpointMetadata("Retrieve a list of all currently active right-hand screen alerts (e.g., 'Need Defenses', 'Major Break Risk').")]
        public async Task GetActiveAlerts(HttpListenerContext context)
        {
            var result = _uiService.GetActiveAlerts();

            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/message")]
        public async Task ShowMessage(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowMessageRequestDto>();
            var result = _windowService.ShowMessage(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/dialog")]
        public async Task ShowDialog(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowDialogRequestDto>();
            var result = _windowService.ShowDialog(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/ui/windows")]
        [EndpointMetadata("List currently open windows and whether each force-pauses the game.")]
        public async Task ListWindows(HttpListenerContext context)
        {
            var result = _windowService.ListWindows();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/window/accept")]
        [EndpointMetadata("Press the accept button of one open window, the same code the game's own button runs, so a dialog's action happens instead of the window being dropped. Body: {window_types: [substrings]} (default: the topmost force-pause window).")]
        public async Task AcceptWindows(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowAcceptRequestDto>();
            var result = _windowService.AcceptWindows(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/ui/letters")]
        [EndpointMetadata("List the letters on the game's letter stack: id, title, first line of the body, kind (LetterDef), arrival tick, whether no earlier reporting read has shown it, and whether the game's own dismiss applies. The read marks the stack seen; add ?peek=1 to look without marking it, which is what the digest and the wait do.")]
        public async Task ListLetters(HttpListenerContext context)
        {
            // peek=1 is the read-only look (ticket #3214): the digest and the
            // wait poll the stack, and a poll that marked it seen left the
            // `letters` read unable to say which letters were new.
            var peek = context.Request.QueryString["peek"];
            var markSeen = !(peek == "1" || string.Equals(peek, "true", System.StringComparison.OrdinalIgnoreCase));
            var result = _letterService.ListLetters(markSeen);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/letter/dismiss")]
        [EndpointMetadata("Dismiss one letter by id through the game's own dismiss, so a letter that was read stops covering the map. Body: {id}. A letter that needs an answer is refused with the reason.")]
        public async Task DismissLetter(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<LetterDismissRequestDto>();
            var result = _letterService.DismissLetter(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/letter/answer")]
        [EndpointMetadata("Open a choice letter (a joiner, a quest offer, a ritual choice) and press the option named in the body, the same code the game's own button runs. Body: {id, option}, where option is a word from the dialog's label (accept, reject); an option that matches none is refused with the labels the dialog offers.")]
        public async Task AnswerLetter(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<LetterAnswerRequestDto>();
            var result = _letterService.AnswerLetter(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/window/close")]        [EndpointMetadata("Close open windows by type name, or (default) every force-pause window — used to auto-dismiss the colony-name and debug-log popups during unattended runs.")]
        public async Task CloseWindows(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowCloseRequestDto>();
            var result = _windowService.CloseWindows(body);
            await context.SendJsonResponse(result);
        }
    }
}