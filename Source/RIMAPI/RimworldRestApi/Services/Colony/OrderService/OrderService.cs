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
        var map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
        if (map == null) return ApiResult.Fail(mapError);

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
        // prevented counts the plants the game refuses to cut, which the cut
        // order leaves standing (fox ticket #4023).
        int prevented = 0;
        // crops counts the colony's own sown plants a cut marks: the game's
        // "Cut plants" order destroys a rice field as readily as wild grass,
        // so a sweep meant to clear ground has to say when it takes the food
        // (fox ticket #4023).
        int crops = 0;
        // The entries the call added, in the shape the designations read
        // sends, so a caller sees what it marked instead of only how many
        // (fox ticket #3530).
        List<DesignationDto> designated = new List<DesignationDto>();
        // The marked targets, for the verdict walk at the end (fox #3625): who
        // will do the work this order leaves behind, and why the rest will not.
        // A sweep can mark hundreds of cells and WorkAcceptance samples a
        // dozen, so a handful of targets is enough to ask.
        List<LocalTargetInfo> targets = new List<LocalTargetInfo>();
        // The products a harvest sweep marked, by def (fox ticket #3648): the
        // caller asks the storage read how much room the map has for them, so
        // a field that stands ripe with a full store says so in the same
        // answer instead of leaving the harvest's fate to a second read.
        List<string> harvestDefs = new List<string>();
        void NoteTarget(LocalTargetInfo target)
        {
            if (targets.Count < 24) targets.Add(target);
        }
        string type = request.Type.ToLower(); // Cache lowercase type
        // The plant orders walk each cell's things anyway, so they count the
        // blight in that walk; only the other orders need a pass of their own
        // (fox ticket #3562). A map-wide sweep used to read every cell's thing
        // list twice.
        bool plantOrder = type == "chop" || type == "cut" || type == "blight";

        foreach (IntVec3 c in rect)
        {
            if (!c.InBounds(map)) continue;
            cells++;
            // So any sweep over a blighted field can say the blight is there
            // (fox ticket #3530).
            if (!plantOrder)
            {
                foreach (var t in c.GetThingList(map))
                {
                    var blightPlant = t as Plant;
                    if (blightPlant != null && blightPlant.Blighted) blighted++;
                }
            }

            if (plantOrder)
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
                    // Counted before the wanted check: a chop sweep marks only
                    // trees, but the blighted rice in the same rectangle has to
                    // show up in the answer all the same (fox ticket #3530).
                    if (plant.Blighted) blighted++;
                    // Cut is the game's own "Cut plants" order: any plant,
                    // tree or grass, which is what a wild field needs before a
                    // growing zone can sow it (fox ticket #4023). Its one
                    // refusal is a plant whose comp forbids cutting, e.g. a
                    // connected Gauranlen tree.
                    bool wanted;
                    if (type == "blight")
                    {
                        wanted = DesignationRules.IsBlightTarget(plant.Blighted);
                    }
                    else if (type == "cut")
                    {
                        bool preventCutting = plant.TryGetComp(out CompPlantPreventCutting prevent) && prevent.PreventCutting;
                        if (preventCutting) prevented++;
                        wanted = DesignationRules.IsCutTarget(preventCutting);
                    }
                    else
                    {
                        wanted = DesignationRules.IsChopTarget(plant.def.plant.IsTree);
                    }
                    if (!wanted) continue;
                    NoteTarget(plant);
                    if (map.designationManager.DesignationOn(plant, DesignationDefOf.CutPlant) != null)
                    {
                        already++;
                        continue;
                    }
                    map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.CutPlant));
                    count++;
                    if (plant.sown) crops++;
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
                    NoteTarget(new LocalTargetInfo(c));
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
                    NoteTarget(t);
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
                    ThingDef product = p.def.plant.harvestedThingDef;
                    if (product != null && !harvestDefs.Contains(product.defName)) harvestDefs.Add(product.defName);
                    NoteTarget(p);
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
                    NoteTarget(p);
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

        // Who will do the work the order leaves behind (fox #3625): the work
        // giver the game would use is asked once per free colonist, so the
        // answer names the colonists whose own check accepts the marked
        // targets and, for the rest, the check that refuses them. A build
        // before #3625 reports neither list, and the caller then keeps its
        // generic sentence.
        List<WorkVerdictDto> workers = new List<WorkVerdictDto>();
        List<WorkVerdictDto> refusals = new List<WorkVerdictDto>();
        WorkGiver_Scanner giver = WorkAcceptance.ScannerFor(DesignationRules.DefNameForOrder(type));
        foreach (WorkAcceptance.Verdict verdict in WorkAcceptance.Walk(map, giver, targets))
        {
            WorkVerdictDto dto = new WorkVerdictDto
            {
                Id = verdict.Pawn.thingIDNumber,
                Name = verdict.Pawn.LabelShort,
                Skill = verdict.Skill,
                SkillLabel = verdict.SkillLabel,
                Reason = verdict.Refusal,
            };
            if (verdict.Refusal == null) workers.Add(dto);
            else refusals.Add(dto);
        }

        // The caller cannot see what was marked, so it gets the numbers: how many
        // cells the rectangle covered, how many designations were added, and how
        // many targets already carried this order. Fox ticket #3530 adds the
        // added entries themselves and the blighted plants the rectangle
        // covers, so a sweep that marked nothing can say why. Fox ticket #3648
        // adds the products the marked plants yield, so the caller can ask the
        // storage read whether the harvest has anywhere to go.
        return ApiResult.Ok(new { cells, designated = count, already, designated_cells = designated, blighted, prevented, crops, workers, refusals, harvest_thing_defs = harvestDefs });
    }
}