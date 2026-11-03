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
        Map map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
        if (map == null)
        {
            return ApiResult.Fail(mapError);
        }
        if (request.PointA == null || request.PointB == null)
        {
            return ApiResult.Fail("point_a and point_b are required: the rectangle to clear.");
        }

        List<DesignationDef> defs = DesignationDefsFor((request.Type ?? "").ToLower());
        if (defs.Count == 0)
        {
            return ApiResult.Fail(
                $"unknown designation type '{request.Type}': use chop, blight, harvest, mine, deconstruct, hunt, smooth or remove-floor");
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
        foreach (DesignationDef def in defs)
        {
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
        }

        // The removed entries travel with the count (ticket #3508): a count
        // alone cannot tell a rectangle of pines from the boulders that were
        // meant, and the answer sends the same shape the designations read
        // does, so a caller compares what left against what it saw. Built
        // before the removal, because RemoveDesignation drops the target.
        List<DesignationDto> removed = new List<DesignationDto>();
        foreach (Designation des in doomed)
        {
            var target = des.target;
            var thing = target.HasThing ? target.Thing : null;
            removed.Add(DesignationEntry.Make(
                des.def?.defName,
                des.def?.label,
                target.HasThing,
                target.Cell.x,
                target.Cell.z,
                thing != null ? thing.thingIDNumber : 0,
                thing?.def?.defName,
                thing?.LabelShort));
        }
        foreach (Designation des in doomed)
        {
            map.designationManager.RemoveDesignation(des);
        }

        return ApiResult.Ok(new { cells, removed = removed.Count, removed_designations = removed });
    }

    // The order's word is the one `designate` takes, so the call that went
    // wrong can be repeated with the other verb. Blight and chop share one
    // designation, so clearing either clears both (fox ticket #3530).
    private static List<DesignationDef> DesignationDefsFor(string type)
    {
        switch (type)
        {
            case "chop":
            case "cut":
            case "blight":
                return new List<DesignationDef> { DesignationDefOf.CutPlant };
            case "harvest":
                return new List<DesignationDef> { DesignationDefOf.HarvestPlant };
            case "mine":
                return new List<DesignationDef> { DesignationDefOf.Mine };
            case "deconstruct":
                return new List<DesignationDef> { DesignationDefOf.Deconstruct };
            case "hunt":
                return new List<DesignationDef> { DesignationDefOf.Hunt };
            // A smooth order leaves one of two designations depending on the
            // cell (a wall or a stone floor), so clearing the word clears both
            // (fox ticket #4316).
            case "smooth":
                return new List<DesignationDef>
                {
                    DesignationDefOf.SmoothFloor,
                    DesignationDefOf.SmoothWall,
                };
            case "remove-floor":
                return new List<DesignationDef> { DesignationDefOf.RemoveFloor };
            default:
                return new List<DesignationDef>();
        }
    }
}
