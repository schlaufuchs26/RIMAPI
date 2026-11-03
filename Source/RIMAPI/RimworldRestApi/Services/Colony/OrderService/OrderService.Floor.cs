using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// The Architect tab's floor orders (fox ticket #4316, parity wave 7). A player
// drags Smooth or Remove floor over a rectangle; both are cell designators
// whose per-cell rules live in FloorWorkRules, and the designations they leave
// behind are the game's own SmoothFloor, SmoothWall and RemoveFloor.
public partial class OrderService
{
    public ApiResult<FloorWorkResultDto> FloorOrder(FloorOrderRequestDto request)
    {
        Map map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
        if (map == null)
        {
            return ApiResult<FloorWorkResultDto>.Fail(mapError);
        }

        if (!FloorWork.IsKnownType(request.Type))
        {
            return ApiResult<FloorWorkResultDto>.Fail(
                $"unknown floor order '{request.Type}': use smooth or remove-floor"
            );
        }

        if (request.PointA == null || request.PointB == null)
        {
            return ApiResult<FloorWorkResultDto>.Fail(
                "point_a and point_b are required: the rectangle to mark."
            );
        }

        IntVec3 a = new IntVec3(request.PointA.X, 0, request.PointA.Z);
        IntVec3 b = new IntVec3(request.PointB.X, 0, request.PointB.Z);
        CellRect rect = CellRect.FromLimits(a, b);

        return ApiResult<FloorWorkResultDto>.Ok(FloorWork.Run(map, request.Type, rect));
    }
}
