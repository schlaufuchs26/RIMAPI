using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;
using Verse;

namespace RIMAPI.Services
{
    public interface IJobLogService
    {
        ApiResult<JobLogDto> GetJobLog(int sinceTick);
    }

    // The read side of the job record: what each colonist did since a tick.
    public class JobLogService : IJobLogService
    {
        public ApiResult<JobLogDto> GetJobLog(int sinceTick)
        {
            var now = Find.TickManager?.TicksGame ?? 0;
            if (sinceTick < 0) sinceTick = 0;
            if (sinceTick > now) sinceTick = now;
            var entries = new List<JobLogEntryDto>();
            foreach (JobLog.Entry e in JobLog.Since(sinceTick))
            {
                entries.Add(new JobLogEntryDto
                {
                    Tick = e.Tick,
                    PawnId = e.PawnId,
                    Pawn = e.Pawn,
                    Job = e.Job,
                    Label = e.Label,
                    Target = e.Target,
                });
            }
            return ApiResult<JobLogDto>.Ok(new JobLogDto
            {
                Now = now,
                Since = sinceTick,
                Entries = entries,
            });
        }
    }
}
