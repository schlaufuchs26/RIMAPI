using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Services.Interfaces;
using RIMAPI.Services;
using RIMAPI.Models;
using RIMAPI.Models.UI;

namespace RIMAPI.BaseControllers
{
    public class UIController
    {
        private readonly IUIService _uiService;
        private readonly IWindowService _windowService;
        private readonly ILetterService _letterService;
        private readonly IMessageService _messageService;

        public UIController(IUIService uiService, IWindowService windowService, ILetterService letterService,
            IMessageService messageService)
        {
            _uiService = uiService;
            _windowService = windowService;
            _letterService = letterService;
            _messageService = messageService;
        }

        [Get("/api/v1/ui/alerts")]
        [EndpointMetadata("List the active screen alerts", Notes = "Retrieve a list of all currently active right-hand screen alerts (e.g., 'Need Defenses', 'Major Break Risk').")]
        [ResponseBody(typeof(List<AlertDto>))]
        public async Task GetActiveAlerts(HttpListenerContext context)
        {
            var result = _uiService.GetActiveAlerts();

            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/message")]
        [RequestBody(typeof(WindowMessageRequestDto))]
        [EndpointMetadata("Show a message window")]
        [ResponseBody]
        public async Task ShowMessage(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowMessageRequestDto>();
            var result = _windowService.ShowMessage(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/dialog")]
        [RequestBody(typeof(WindowDialogRequestDto))]
        [EndpointMetadata("Show a dialog window")]
        [ResponseBody]
        public async Task ShowDialog(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowDialogRequestDto>();
            var result = _windowService.ShowDialog(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/ui/windows")]
        [EndpointMetadata("List the open windows", Notes = "List currently open windows and whether each force-pauses the game.")]
        [ResponseBody(typeof(List<OpenWindowDto>))]
        public async Task ListWindows(HttpListenerContext context)
        {
            var result = _windowService.ListWindows();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/window/accept")]
        [RequestBody(typeof(WindowAcceptRequestDto))]
        [EndpointMetadata("Press a window's accept button", Notes = "Press the accept button of one open window, the same code the game's own button runs, so a dialog's action happens instead of the window being dropped. Body: {window_types: [substrings]} (default: the topmost force-pause window).")]
        [ResponseBody(typeof(WindowAcceptResultDto))]
        public async Task AcceptWindows(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowAcceptRequestDto>();
            var result = _windowService.AcceptWindows(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/ui/letters")]
        [QueryParameters("archive:string?=When true, read the archived letters instead of the live stack.", "limit:integer?", "peek:string?=Which field of each letter to read, by name.", "since:integer?")]
        [EndpointMetadata("List the letters on the stack", Notes = "List the letters on the game's letter stack: id, title, first line of the body, kind (LetterDef), arrival tick, whether no earlier reporting read has shown it, whether the game's own dismiss applies, and whether the game's auto-pause rule pauses the clock for it. The read marks the stack seen; add ?peek=1 to look without marking it, which is what the digest and the wait do. Add ?archive=1 to read the letters the game received instead of the ones still standing (query: since=<tick>, limit=<n>): the stack drops a letter whose target is gone, so a letter that paused the game can be off it by the next look.")]
        [ResponseBody(typeof(List<LetterDto>))]
        public async Task ListLetters(HttpListenerContext context)
        {
            // peek=1 is the read-only look (ticket #3214): the digest and the
            // wait poll the stack, and a poll that marked it seen left the
            // `letters` read unable to say which letters were new.
            var peek = context.Request.QueryString["peek"];
            var markSeen = !(peek == "1" || string.Equals(peek, "true", System.StringComparison.OrdinalIgnoreCase));
            var archive = context.Request.QueryString["archive"];
            if (archive == "1" || string.Equals(archive, "true", System.StringComparison.OrdinalIgnoreCase))
            {
                // The archive branch (ticket #3357): what the game received,
                // standing or not, so a pause a vanished letter caused has a
                // name.
                int.TryParse(context.Request.QueryString["since"], out var since);
                int.TryParse(context.Request.QueryString["limit"], out var limit);
                var archived = _letterService.ListArchivedLetters(since, limit);
                await context.SendJsonResponse(archived);
                return;
            }
            var result = _letterService.ListLetters(markSeen);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/ui/messages")]
        [QueryParameters("limit:integer?", "since:integer?")]
        [EndpointMetadata("List the game's messages", Notes = "List the game's own message archive: the little notifications (a plant died of cold, blight struck, an animal roams away, a colonist was attacked) with their text, def, tick and the thing or cell they are about. Query: since=<tick> for only the messages created at or after it, limit=<n> for the newest few (default 25, max 200). Letters have their own read and are not in this list.")]
        [ResponseBody(typeof(List<MessageDto>))]
        public async Task ListMessages(HttpListenerContext context)
        {
            int.TryParse(context.Request.QueryString["since"], out var since);
            int.TryParse(context.Request.QueryString["limit"], out var limit);
            var result = _messageService.ListMessages(since, limit);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/letter/dismiss")]
        [RequestBody(typeof(LetterDismissRequestDto))]
        [EndpointMetadata("Dismiss a letter", Notes = "Dismiss one letter by id through the game's own dismiss, so a letter that was read stops covering the map. Body: {id}. A letter that needs an answer is refused with the reason.")]
        [ResponseBody(typeof(LetterDismissResultDto))]
        public async Task DismissLetter(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<LetterDismissRequestDto>();
            var result = _letterService.DismissLetter(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/letter/answer")]
        [RequestBody(typeof(LetterAnswerRequestDto))]
        [EndpointMetadata("Press a choice letter's option", Notes = "Open a choice letter (a joiner, a quest offer, a ritual choice) and press the option named in the body, the same code the game's own button runs. Body: {id, option}, where option is a word from the dialog's label (accept, reject); an option that matches none is refused with the labels the dialog offers.")]
        [ResponseBody(typeof(LetterAnswerResultDto))]
        public async Task AnswerLetter(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<LetterAnswerRequestDto>();
            var result = _letterService.AnswerLetter(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/letter/growth")]
        [RequestBody(typeof(LetterGrowthRequestDto))]
        [EndpointMetadata("Take a growth letter's choice", Notes = "Take the choice a growth letter asks for: a child's 'became an adult' letter (ChoiceLetter_GrowthMoment) wants a skill that gains a passion and, usually, one trait. Body: {id, passion, trait}, where passion names a skill from passion_choices or its 0-based index, and trait names one of trait_choices or 'none' for the letter's own no-trait option. A call with only id answers what the letter offers and applies nothing. The choice runs the game's own MakeChoices, the method the letter's dialog calls when its confirm button is pressed, so the pawn gains the passion and the trait and the letter leaves the stack.")]
        [ResponseBody(typeof(LetterGrowthResultDto))]
        public async Task ApplyGrowthChoice(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<LetterGrowthRequestDto>();
            var result = _letterService.ApplyGrowthChoice(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ui/window/close")]
        [EndpointMetadata("Close open windows", Notes = "Close open windows by type name, or (default) every force-pause window — used to auto-dismiss the colony-name and debug-log popups during unattended runs.")]
        [RequestBody(typeof(WindowCloseRequestDto))]
        [ResponseBody(typeof(WindowCloseResultDto))]
        public async Task CloseWindows(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WindowCloseRequestDto>();
            var result = _windowService.CloseWindows(body);
            await context.SendJsonResponse(result);
        }
    }
}