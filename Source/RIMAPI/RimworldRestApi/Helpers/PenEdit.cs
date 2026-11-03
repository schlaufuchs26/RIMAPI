using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The animal pen's own controls (fox ticket #4316, parity wave 8): the
    /// Animals tab's filter of which animal kinds the pen takes, the Auto-cut
    /// tab's checkbox and its "Auto-cut now" button, and the marker's name. All
    /// three are the game's own objects (CompAnimalPenMarker.animalFilter,
    /// CompAutoCut.autoCut, DesignatePlantsToCut, RenamableLabel), so a caller
    /// changes what the player changes and nothing else.
    /// </summary>
    public static class PenEdit
    {
        public static CompAnimalPenMarker FindPen(Map map, int markerId, out string error)
        {
            error = null;
            Thing thing = map.listerThings.AllThings.FirstOrDefault(
                t => t.thingIDNumber == markerId
            );
            if (thing == null)
            {
                error =
                    $"No thing with id {markerId} lies on map {map.uniqueID}. "
                    + "GET /api/v1/map/pens lists the pens with their marker_id.";
                return null;
            }

            CompAnimalPenMarker marker = (thing as ThingWithComps)?.GetComp<CompAnimalPenMarker>();
            if (marker == null)
            {
                error =
                    $"Thing {markerId} ({thing.def.defName}) is not an animal pen marker; "
                    + "GET /api/v1/map/pens lists the pens with their marker_id.";
                return null;
            }

            return marker;
        }

        /// <summary>
        /// The Animals tab: the pen's filter decides which animal kinds it
        /// takes. The game's own fixed filter bounds it (only rope-managed
        /// animals appear there), so a name outside that set is reported as
        /// unknown instead of silently ignored.
        /// </summary>
        public static ApiResult<PenEditResultDto> SetAnimals(
            Map map,
            CompAnimalPenMarker marker,
            PenAnimalsRequestDto request
        )
        {
            ThingFilter filter = marker.AnimalFilter;
            if (filter == null)
            {
                return ApiResult<PenEditResultDto>.Fail(
                    $"Pen {marker.parent.LabelShort} carries no animal filter."
                );
            }

            if (
                (request.Allow == null || request.Allow.Count == 0)
                && (request.Deny == null || request.Deny.Count == 0)
            )
            {
                return ApiResult<PenEditResultDto>.Fail(
                    "the body names no animal: send allow, deny, or both, each a list of animal defNames"
                );
            }

            var result = NewResult(marker);
            MapHelper.ApplyFilterNames(
                filter,
                request.Allow,
                true,
                result.AppliedDefs,
                result.AppliedCategories,
                result.Unknown
            );
            MapHelper.ApplyFilterNames(
                filter,
                request.Deny,
                false,
                result.AppliedDefs,
                result.AppliedCategories,
                result.Unknown
            );

            Fill(result, marker);
            return ApiResult<PenEditResultDto>.Ok(result);
        }

        /// <summary>
        /// The Auto-cut tab: the checkbox, and the "Auto-cut now" button that
        /// marks every plant the filter allows. The game greys the button out
        /// while the pen is not enclosed, so the call refuses the same way.
        /// </summary>
        public static ApiResult<PenEditResultDto> SetAutoCut(
            Map map,
            CompAnimalPenMarker marker,
            PenAutoCutRequestDto request
        )
        {
            if (!request.Enabled.HasValue && !request.CutNow)
            {
                return ApiResult<PenEditResultDto>.Fail(
                    "the body names nothing to do: send enabled (the checkbox) or cut_now (the button)"
                );
            }

            var result = NewResult(marker);
            if (request.Enabled.HasValue)
            {
                marker.autoCut = request.Enabled.Value;
            }

            if (request.CutNow)
            {
                PenMarkerState state = marker.PenState;
                if (state == null || !state.Enclosed)
                {
                    return ApiResult<PenEditResultDto>.Fail(
                        $"The Auto-cut tab refuses to cut in {marker.InspectLabel}: the pen is not enclosed (AutocutUnenclosedPen)."
                    );
                }

                marker.DesignatePlantsToCut();
                result.CutMarked = true;
            }

            Fill(result, marker);
            return ApiResult<PenEditResultDto>.Ok(result);
        }

        /// <summary>
        /// The marker's rename, under Dialog_RenameAnimalPen's rules: an empty
        /// name and a name another pen already carries are refused. The rule
        /// itself is the rename dialog's, shared with zones and areas.
        /// </summary>
        public static ApiResult<PenEditResultDto> Rename(
            Map map,
            CompAnimalPenMarker marker,
            PenRenameRequestDto request
        )
        {
            // Dialog_RenameAnimalPen asks animalPenManager.GetPenNamed(name),
            // which compares the raw `label` a player typed, not the label a pen
            // falls back to; an unnamed pen therefore does not hold a name.
            List<string> taken = PenMarkers(map)
                .Where(pen => pen != marker && !string.IsNullOrEmpty(pen.label))
                .Select(pen => pen.label)
                .ToList();
            string refusal = ZoneEditRules.LabelRefusal(request.Label, taken);
            if (refusal != null)
            {
                return ApiResult<PenEditResultDto>.Fail(refusal);
            }

            string before = marker.InspectLabel;
            marker.RenamableLabel = request.Label;
            LogApi.Info($"Renamed pen '{before}' to '{request.Label}'");

            var result = NewResult(marker);
            Fill(result, marker);
            return ApiResult<PenEditResultDto>.Ok(result);
        }

        private static IEnumerable<CompAnimalPenMarker> PenMarkers(Map map)
        {
            ThingDef markerDef = DefDatabase<ThingDef>.GetNamedSilentFail("PenMarker");
            if (markerDef == null)
            {
                yield break;
            }

            foreach (Thing thing in map.listerThings.ThingsOfDef(markerDef))
            {
                CompAnimalPenMarker marker = (thing as ThingWithComps)?.GetComp<CompAnimalPenMarker>();
                if (marker != null)
                {
                    yield return marker;
                }
            }
        }

        private static PenEditResultDto NewResult(CompAnimalPenMarker marker)
        {
            return new PenEditResultDto
            {
                MarkerId = marker.parent.thingIDNumber,
                AppliedDefs = new List<string>(),
                AppliedCategories = new List<string>(),
                Unknown = new List<string>(),
            };
        }

        private static void Fill(PenEditResultDto result, CompAnimalPenMarker marker)
        {
            result.Label = marker.InspectLabel;
            result.AutoCut = marker.autoCut;
            result.AnimalsAllowed = marker
                .AnimalFilter.AllowedThingDefs.Select(def => def.defName)
                .OrderBy(name => name)
                .ToList();
        }
    }
}
