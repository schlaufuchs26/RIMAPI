using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Services;
using RimWorld;
using Verse;

public partial class OrderService : IOrderService
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
        // blighted counts the blighted plants the rectangle covers, whatever
        // the order was: a chop sweep over a rice field marks nothing, and the
        // answer has to say why (fox ticket #3530).
        int blighted = 0;
        // The entries the call added, in the shape the designations read
        // sends, so a caller sees what it marked instead of only how many
        // (fox ticket #3530).
        List<DesignationDto> designated = new List<DesignationDto>();
        string type = request.Type.ToLower(); // Cache lowercase type

        foreach (IntVec3 c in rect)
        {
            if (!c.InBounds(map)) continue;
            cells++;
            // Counted before the order branches, so any sweep over a blighted
            // field can say the blight is there (fox ticket #3530).
            foreach (var t in c.GetThingList(map))
            {
                var blightPlant = t as Plant;
                if (blightPlant != null && blightPlant.Blighted) blighted++;
            }

            if (type == "chop" || type == "cut" || type == "blight")
            {
                // Chop wood is the game's CutPlant designation (Designator_PlantsHarvestWood);
                // HarvestPlant is the harvest order and does nothing on a tree, so marking a
                // tree as Harvest is a silent no-op (fox ticket #3058).
                //
                // Blight is the same designation and a different rule: the
                // game's "cut the affected plants" of the blight letter cuts
                // any plant the blight infects, tree or crop (fox ticket #3530).
                foreach (var t in c.GetThingList(map))
                {
                    var plant = t as Plant;
                    if (plant == null) continue;
                    bool wanted = type == "blight"
                        ? DesignationRules.IsBlightTarget(plant.Blighted)
                        : DesignationRules.IsChopTarget(plant.def.plant.IsTree);
                    if (!wanted) continue;
                    if (map.designationManager.DesignationOn(plant, DesignationDefOf.CutPlant) != null)
                    {
                        already++;
                        continue;
                    }
                    map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.CutPlant));
                    count++;
                    designated.Add(DesignationEntry.Make(DesignationDefOf.CutPlant.defName, DesignationDefOf.CutPlant.label,
                        true, plant.Position.x, plant.Position.z, plant.thingIDNumber, plant.def.defName, plant.LabelShort));
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
                        designated.Add(DesignationEntry.Make(DesignationDefOf.Mine.defName, DesignationDefOf.Mine.label,
                            false, c.x, c.z));
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
                    designated.Add(DesignationEntry.Make(DesignationDefOf.Deconstruct.defName, DesignationDefOf.Deconstruct.label,
                        true, t.Position.x, t.Position.z, t.thingIDNumber, t.def.defName, t.LabelShort));
                }
            }
            else if (type == "harvest")
            {
                var plants = c.GetThingList(map).Where(t => t.def.category == ThingCategory.Plant).Cast<Plant>();
                foreach (var p in plants)
                {
                    // The game's own harvest designator takes HarvestableNow
                    // plus the "Standard" harvest tag; a tree carries "Wood"
                    // and belongs to the chop order (fox ticket #3425).
                    if (!DesignationRules.IsHarvestTarget(p.HarvestableNow, p.def.plant.harvestTag)) continue;
                    if (map.designationManager.DesignationOn(p, DesignationDefOf.HarvestPlant) != null)
                    {
                        already++;
                        continue;
                    }
                    map.designationManager.AddDesignation(new Designation(p, DesignationDefOf.HarvestPlant));
                    count++;
                    designated.Add(DesignationEntry.Make(DesignationDefOf.HarvestPlant.defName, DesignationDefOf.HarvestPlant.label,
                        true, p.Position.x, p.Position.z, p.thingIDNumber, p.def.defName, p.LabelShort));
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
                    designated.Add(DesignationEntry.Make(DesignationDefOf.Hunt.defName, DesignationDefOf.Hunt.label,
                        true, p.Position.x, p.Position.z, p.thingIDNumber, p.def.defName, p.LabelShort));
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
        // many targets already carried this order. Fox ticket #3530 adds the
        // added entries themselves and the blighted plants the rectangle
        // covers, so a sweep that marked nothing can say why.
        return ApiResult.Ok(new { cells, designated = count, already, designated_cells = designated, blighted });
    }
}