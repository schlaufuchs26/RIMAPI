using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Services;
using RimWorld;
using Verse;

public class OrderService : IOrderService
{
    public ApiResult DesignateArea(DesignateRequestDto request)
    {
        var map = MapHelper.GetMapByID(request.MapId);
        if (map == null) return ApiResult.Fail($"Map with id: {request.MapId} not found");

        IntVec3 start = new IntVec3(request.PointA.X, 0, request.PointA.Z);
        IntVec3 end = new IntVec3(request.PointB.X, 0, request.PointB.Z);
        CellRect rect = CellRect.FromLimits(start, end);

        int count = 0;
        int cells = 0;
        // already counts the targets the rectangle covered that carried this
        // designation before the call, so a second call answers "nothing new"
        // instead of "nothing there" (fox ticket #3060).
        int already = 0;
        string type = request.Type.ToLower(); // Cache lowercase type

        foreach (IntVec3 c in rect)
        {
            if (!c.InBounds(map)) continue;
            cells++;

            if (type == "chop" || type == "cut")
            {
                // Chop wood is the game's CutPlant designation (Designator_PlantsHarvestWood);
                // HarvestPlant is the harvest order and does nothing on a tree, so marking a
                // tree as Harvest is a silent no-op (fox ticket #3058).
                foreach (var t in c.GetThingList(map))
                {
                    var plant = t as Plant;
                    if (plant == null || !plant.def.plant.IsTree) continue;
                    if (map.designationManager.DesignationOn(plant, DesignationDefOf.CutPlant) != null)
                    {
                        already++;
                        continue;
                    }
                    map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.CutPlant));
                    count++;
                }
            }
            else if (type == "mine")
            {
                // Add Mining Designation
                var edifice = c.GetEdifice(map);
                if (edifice != null && edifice.def.mineable)
                {
                    if (map.designationManager.DesignationAt(c, DesignationDefOf.Mine) != null)
                    {
                        already++;
                    }
                    else
                    {
                        map.designationManager.AddDesignation(new Designation(c, DesignationDefOf.Mine));
                        count++;
                    }
                }
            }
            else if (type == "deconstruct")
            {
                // Find buildings to deconstruct
                var things = c.GetThingList(map).ToList();
                foreach (var t in things)
                {
                    if (t.def.category != ThingCategory.Building) continue;
                    if (map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
                    {
                        already++;
                        continue;
                    }
                    map.designationManager.AddDesignation(new Designation(t, DesignationDefOf.Deconstruct));
                    count++;
                }
            }
            else if (type == "harvest")
            {
                var plants = c.GetThingList(map).Where(t => t.def.category == ThingCategory.Plant).Cast<Plant>();
                foreach (var p in plants)
                {
                    if (!p.HarvestableNow) continue;
                    if (map.designationManager.DesignationOn(p, DesignationDefOf.HarvestPlant) != null)
                    {
                        already++;
                        continue;
                    }
                    map.designationManager.AddDesignation(new Designation(p, DesignationDefOf.HarvestPlant));
                    count++;
                }
            }
            else if (type == "hunt")
            {
                // Find animals to hunt
                var pawns = c.GetThingList(map).OfType<Pawn>();
                foreach (var p in pawns)
                {
                    // Check if it's an animal, not our colony pet, and not already marked
                    if (!p.RaceProps.Animal || p.Faction == Faction.OfPlayer) continue;
                    if (map.designationManager.DesignationOn(p, DesignationDefOf.Hunt) != null)
                    {
                        already++;
                        continue;
                    }
                    map.designationManager.AddDesignation(new Designation(p, DesignationDefOf.Hunt));
                    count++;
                }
            }
            else if (type == "remove-all")
            {
                var things = c.GetThingList(map);
                foreach (var t in things)
                {
                    map.designationManager.RemoveAllDesignationsOn(t);
                }
                count++;
            }
        }

        // The caller cannot see what was marked, so it gets the numbers: how many
        // cells the rectangle covered, how many designations were added, and how
        // many targets already carried this order.
        return ApiResult.Ok(new { cells, designated = count, already });
    }
}