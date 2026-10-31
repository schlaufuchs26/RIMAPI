using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

public partial class OrderService
{
    // The other half of a designate call (fox ticket #3425): a sweep over the
    // wrong rectangle used to be permanent, because `cancel` takes back builds
    // and nothing removed a designation. On colony 12 one sweep marked 770
    // trees as HarvestPlant, and those cells stayed in the work list for good.
    // The order below removes one kind over a rectangle, so a caller repeats
    // the call that went wrong with the other verb.
    public ApiResult UndesignateArea(DesignateRequestDto request)
    {
        Map map = MapHelper.GetMapByID(request.MapId);
        if (map == null)
        {
            return ApiResult.Fail($"Map with id: {request.MapId} not found");
        }
        if (request.PointA == null || request.PointB == null)
        {
            return ApiResult.Fail("point_a and point_b are required: the rectangle to clear.");
        }

        DesignationDef def = DesignationDefFor((request.Type ?? "").ToLower());
        if (def == null)
        {
            return ApiResult.Fail(
                $"unknown designation type '{request.Type}': use chop, harvest, mine, deconstruct or hunt");
        }

        IntVec3 a = new IntVec3(request.PointA.X, 0, request.PointA.Z);
        IntVec3 b = new IntVec3(request.PointB.X, 0, request.PointB.Z);
        CellRect rect = CellRect.FromLimits(a, b);

        int cells = 0;
        foreach (IntVec3 c in rect)
        {
            if (c.InBounds(map))
            {
                cells++;
            }
        }

        // Collect before removing: RemoveDesignation edits the list this
        // enumeration walks.
        List<Designation> doomed = new List<Designation>();
        foreach (Designation des in map.designationManager.SpawnedDesignationsOfDef(def))
        {
            CellRect target = des.target.HasThing
                ? des.target.Thing.OccupiedRect()
                : CellRect.SingleCell(des.target.Cell);
            if (!DesignationRules.Covers(target.minX, target.minZ, target.maxX, target.maxZ,
                    rect.minX, rect.minZ, rect.maxX, rect.maxZ))
            {
                continue;
            }
            doomed.Add(des);
        }
        foreach (Designation des in doomed)
        {
            map.designationManager.RemoveDesignation(des);
        }

        return ApiResult.Ok(new { cells, removed = doomed.Count });
    }

    // The order's word is the one `designate` takes, so the call that went
    // wrong can be repeated with the other verb.
    private static DesignationDef DesignationDefFor(string type)
    {
        switch (type)
        {
            case "chop":
            case "cut":
                return DesignationDefOf.CutPlant;
            case "harvest":
                return DesignationDefOf.HarvestPlant;
            case "mine":
                return DesignationDefOf.Mine;
            case "deconstruct":
                return DesignationDefOf.Deconstruct;
            case "hunt":
                return DesignationDefOf.Hunt;
            default:
                return null;
        }
    }
}
