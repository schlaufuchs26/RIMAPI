using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class JobLogController
    {
        private readonly IJobLogService _jobLogService;

        public JobLogController(IJobLogService jobLogService)
        {
            _jobLogService = jobLogService;
        }

        // What each colonist did since a tick: the record the wait report folds
        // into "Tilger: harvest rice 1h 20m, idle 4h" (ticket #3300). since_tick
        // is the tick the caller's span starts at; the answer also carries the
        // entry each pawn was in at that tick, so the first span has a start.
        [Get("/api/v1/colonists/job-log")]
        [EndpointMetadata("Job changes of the colony's colonists since a tick: pawn, job def, label, target")]
        public async Task GetJobLog(HttpListenerContext context)
        {
            string raw = RequestParser.GetStringParameter(context, "since_tick", false);
            int sinceTick = 0;
            if (!string.IsNullOrEmpty(raw) && !int.TryParse(raw, out sinceTick))
            {
                await context.SendError(
                    HttpStatusCode.BadRequest,
                    $"since_tick must be a game tick, not \"{raw}\""
                );
                return;
            }
            var result = _jobLogService.GetJobLog(sinceTick);
            await context.SendJsonResponse(result);
        }
    }
}
