using System;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Verse;

namespace RIMAPI.Services
{
    // The Health tab's "Allow self-tend" option is a checkbox, and a colony of
    // one lives or dies by it: WorkGiver_TendSelf hands a self-tend job out only
    // while it is on, and JobDriver_TendPatient ends the job when it is off. The
    // tool could queue that job but not move the checkbox, so Tilger lay downed
    // and bleeding at 72 % with a shelf of medicine beside him (ticket #3279).
    // This is the write; nothing else about the pawn changes.
    public partial class ColonistService
    {
        public ApiResult SetSelfTend(int pawnId, bool enabled)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                {
                    return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                }
                if (!pawn.RaceProps.Humanlike)
                {
                    return ApiResult.Fail(
                        $"Pawn {pawnId} has no Self-tend option: only humanlikes have one"
                    );
                }
                if (pawn.playerSettings == null)
                {
                    return ApiResult.Fail(
                        $"Pawn {pawnId} has no player settings, so it has no Self-tend option"
                    );
                }
                bool was = pawn.playerSettings.selfTend;
                pawn.playerSettings.selfTend = enabled;
                return ApiResult.Ok(
                    new SelfTendResultDto
                    {
                        PawnId = pawnId,
                        Name = pawn.LabelShortCap,
                        SelfTend = enabled,
                        Changed = was != enabled,
                    }
                );
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }
    }
}
