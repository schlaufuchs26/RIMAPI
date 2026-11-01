using System;
using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// What the colony has marked and what it is building: the
// designations and the blueprints and frames (fox #3659).

namespace RIMAPI.Services
{
    public partial class MapService
    {
        /// <summary>
        /// Every designation the map currently carries: what is marked for
        /// chopping, mining, deconstructing, harvesting or hunting, and where.
        /// The manager holds both the saved and the spawned designations, so
        /// the answer is the same set the game draws (fox ticket #3060).
        /// </summary>
        public ApiResult<List<DesignationDto>> GetMapDesignations(int mapId)
        {
            var map = MapHelper.GetMapByID(mapId);
            if (map == null)
            {
                return ApiResult<List<DesignationDto>>.Fail($"Map not found: {mapId}");
            }

            var result = new List<DesignationDto>();
            foreach (var designation in map.designationManager.AllDesignations)
            {
                // A thing-targeted designation names the thing, so a caller can
                // address the same thing again (the ids the things read prints).
                var target = designation.target;
                var thing = target.HasThing ? target.Thing : null;
                result.Add(DesignationEntry.Make(
                    designation.def?.defName,
                    designation.def?.label,
                    target.HasThing,
                    target.Cell.x,
                    target.Cell.z,
                    thing != null ? thing.thingIDNumber : 0,
                    thing?.def?.defName,
                    thing?.LabelShort));
            }

            return ApiResult<List<DesignationDto>>.Ok(result);
        }

        /// <summary>
        /// Every blueprint and frame on the map: what will stand where and how
        /// much work is left. A build order used to be unverifiable, because
        /// the buildings read lists only finished buildings (fox ticket #3089).
        /// The group reads see both kinds; a blueprint's work is its whole work,
        /// because nothing of it is built yet.
        /// </summary>
        public ApiResult<List<BlueprintEntryDto>> GetMapBlueprints(int mapId)
        {
            var map = MapHelper.GetMapByID(mapId);
            if (map == null)
            {
                return ApiResult<List<BlueprintEntryDto>>.Fail($"Map not found: {mapId}");
            }

            var result = new List<BlueprintEntryDto>();

            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
            {
                var blueprint = thing as Blueprint;
                var target = blueprint?.EntityToBuild();
                if (target == null)
                {
                    continue;
                }

                // Only a Blueprint_Build carries the material; a floor blueprint
                // is the plain base class and has none (ticket #3089).
                var build = blueprint as Blueprint_Build;
                // The blueprint's own WorkTotal is protected; a blueprint has no
                // work done yet, so the def's build cost is the work left.
                var work = target.GetStatValueAbstract(StatDefOf.WorkToBuild, build?.stuffToUse);
                // A blueprint that never gets its material is invisible as such:
                // the read showed the def and the work left, so thirteen granite
                // walls read like thirteen walls (ticket #3133). The counter is
                // the colony's own count of what lies on the map.
                var stuffDef = build?.stuffToUse;
                result.Add(new BlueprintEntryDto
                {
                    DefName = target.defName,
                    Label = target.LabelCap,
                    Kind = "blueprint",
                    Stuff = stuffDef?.defName,
                    X = blueprint.Position.x,
                    Z = blueprint.Position.z,
                    Rotation = blueprint.Rotation.AsInt,
                    Size = BlueprintFootprint.Of(target),
                    WorkLeft = work,
                    WorkTotal = work,
                    ThingId = blueprint.thingIDNumber,
                    MaterialNeeded = stuffDef == null ? 0 : target.CostStuffCount,
                    MaterialAvailable = stuffDef == null ? 0 : map.resourceCounter.GetCount(stuffDef),
                });
            }

            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
            {
                var frame = thing as Frame;
                if (frame?.BuildDef == null)
                {
                    continue;
                }

                // A frame already holds the material a colonist hauled into it,
                // so what it still needs is the rest of its cost, and the map's
                // counter says whether that rest exists (ticket #3133).
                var frameStuff = frame.EntityToBuildStuff();
                int delivered = 0;
                if (frameStuff != null)
                {
                    var held = frame.GetDirectlyHeldThings();
                    if (held != null)
                    {
                        delivered = held.TotalStackCount;
                    }
                }

                result.Add(new BlueprintEntryDto
                {
                    DefName = frame.BuildDef.defName,
                    Label = frame.LabelEntityToBuild,
                    Kind = "frame",
                    Stuff = frameStuff?.defName,
                    X = frame.Position.x,
                    Z = frame.Position.z,
                    Rotation = frame.Rotation.AsInt,
                    Size = BlueprintFootprint.Of(frame.BuildDef),
                    WorkLeft = frame.WorkLeft,
                    WorkTotal = frame.WorkToBuild,
                    ThingId = frame.thingIDNumber,
                    MaterialNeeded = frameStuff == null ? 0 : Math.Max(0, frame.BuildDef.CostStuffCount - delivered),
                    MaterialAvailable = frameStuff == null ? 0 : map.resourceCounter.GetCount(frameStuff),
                });
            }

            return ApiResult<List<BlueprintEntryDto>>.Ok(result);
        }
    }
}
