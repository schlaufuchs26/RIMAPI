using System;
using System.Collections.Generic;
using RimWorld;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Verse;

namespace RIMAPI.Services
{
    // The colony's work settings in one read (fox ticket #4158). `build` and
    // `blueprint` name who would build a plan, which cost one
    // /api/v1/colonist/work-priorities call per colonist: eight pawns, eight
    // loopback reads on every placement. This route answers the same numbers
    // for every colonist in a single call.
    public partial class ColonistService
    {
        public ApiResult<ColonistsWorkPrioritiesDto> GetColonistsWorkPriorities()
        {
            try
            {
                var result = new ColonistsWorkPrioritiesDto();
                foreach (Pawn pawn in ColonistsHelper.GetColonistsList())
                {
                    if (pawn == null)
                        continue;
                    result.Pawns.Add(WorkPrioritiesOf(pawn));
                }

                result.TotalPawns = result.Pawns.Count;
                return ApiResult<ColonistsWorkPrioritiesDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<ColonistsWorkPrioritiesDto>.Fail(ex.Message);
            }
        }

        // WorkPrioritiesOf fills one pawn's entry. The single-pawn route and the
        // colony route both call it, so the two answers cannot drift apart.
        private static ColonistWorkPrioritiesDto WorkPrioritiesOf(Pawn pawn)
        {
            var readiness = WorkSettingsRules.Of(
                hasWorkSettings: pawn.workSettings != null,
                everWork: pawn.workSettings != null && pawn.workSettings.EverWork
            );

            var result = new ColonistWorkPrioritiesDto
            {
                PawnId = pawn.thingIDNumber,
                PawnName = pawn.LabelShortCap,
                ManualPriorities = Find.PlaySettings?.useWorkPriorities ?? false,
                Initialized = readiness == WorkSettingsRules.Readiness.Ready,
                Readiness = WorkSettingsRules.Name(readiness),
            };

            if (!result.Initialized)
            {
                return result;
            }

            DefMap<WorkTypeDef, int> stored = StoredWorkPriorities(pawn);
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if (workType == null)
                    continue;
                int priority = pawn.workSettings.GetPriority(workType);
                result.Work.Add(
                    new WorkPriorityEntryDto
                    {
                        WorkType = workType.defName,
                        Priority = priority,
                        Stored = stored != null ? stored[workType] : priority,
                        Disabled = pawn.WorkTypeIsDisabled(workType),
                    }
                );
            }

            return result;
        }
    }
}
