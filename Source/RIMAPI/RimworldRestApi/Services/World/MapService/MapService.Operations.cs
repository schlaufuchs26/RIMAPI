using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// The routes that change the map directly, without an order: they
// destroy, repair or drop something in (fox #3659).

namespace RIMAPI.Services
{
    public partial class MapService
    {
        /// <summary>
        /// Destroys all corpses (human and animal) on the specified map.
        /// </summary>
        public ApiResult DestroyCorpses(int mapId)
        {
            try
            {
                var map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail(MapLookup.MapNotFound(mapId));

                // Fetch all corpses efficiently using the map's ThingLister
                // ToList() is required because we are modifying the collection while iterating
                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).ToList();
                int count = corpses.Count;

                foreach (var corpse in corpses)
                {
                    if (!corpse.Destroyed)
                    {
                        corpse.Destroy();
                    }
                }

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error destroying corpses: {ex}");
                return ApiResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Destroys all items on the map that are marked as Forbidden (Red X).
        /// </summary>
        public ApiResult DestroyForbiddenItems(int mapId)
        {
            try
            {
                var map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail(MapLookup.MapNotFound(mapId));

                // "HaulableEver" covers almost all items (resources, weapons, apparel)
                var items = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
                var toDestroy = new List<Thing>();

                // Filter for forbidden items
                foreach (var item in items)
                {
                    if (item.IsForbidden(Faction.OfPlayer))
                    {
                        toDestroy.Add(item);
                    }
                }

                int count = toDestroy.Count;
                foreach (var item in toDestroy)
                {
                    if (!item.Destroyed)
                    {
                        item.Destroy();
                    }
                }

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error destroying forbidden items: {ex}");
                return ApiResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Destroys ALL things (Items, Buildings, Pawns, Plants, Filth) within a rectangular area.
        /// Does not destroy the Terrain (Floor).
        /// </summary>
        public ApiResult DestroyThingsInRect(DestroyRectRequestDto request)
        {
            try
            {
                if (request.PointA == null || request.PointB == null)
                    return ApiResult.Fail("PointA and PointB must be defined.");

                var map = MapHelper.GetMapByID(request.MapId);
                if (map == null) return ApiResult.Fail(MapLookup.MapNotFound(request.MapId));

                // Create the rectangle defined by the two points
                IntVec3 start = new IntVec3(request.PointA.X, 0, request.PointA.Z);
                IntVec3 end = new IntVec3(request.PointB.X, 0, request.PointB.Z);
                CellRect rect = CellRect.FromLimits(start, end);

                int count = 0;

                // Iterate through every cell in the rectangle
                foreach (IntVec3 cell in rect)
                {
                    if (!cell.InBounds(map)) continue;

                    // Get all things in this single cell
                    // CRITICAL: Create a copy (.ToList()) because destroying removes them from the list we are iterating
                    var thingsInCell = cell.GetThingList(map).ToList();

                    foreach (var thing in thingsInCell)
                    {
                        if (!thing.Destroyed)
                        {
                            thing.Destroy();
                            count++;
                        }
                    }
                }

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error destroying area: {ex}");
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult RepairThingsAtPositions(RepairPositionsRequestDto request)
        {
            try
            {
                var map = MapHelper.GetMapByID(request.MapId);
                if (map == null) return ApiResult.Fail(MapLookup.MapNotFound(request.MapId));

                int count = 0;
                foreach (var posDto in request.Positions)
                {
                    IntVec3 cell = new IntVec3(posDto.X, posDto.Y, posDto.Z);
                    if (cell.InBounds(map))
                    {
                        count += RepairThingsInCell(cell, map);
                    }
                }
                return ApiResult.Ok();
            }
            catch (Exception ex) { return ApiResult.Fail(ex.Message); }
        }

        public ApiResult RepairThingsInRect(RepairRectRequestDto request)
        {
            try
            {
                var map = MapHelper.GetMapByID(request.MapId);
                if (map == null) return ApiResult.Fail(MapLookup.MapNotFound(request.MapId));

                IntVec3 start = new IntVec3(request.PointA.X, 0, request.PointA.Z);
                IntVec3 end = new IntVec3(request.PointB.X, 0, request.PointB.Z);
                CellRect rect = CellRect.FromLimits(start, end);

                int count = 0;
                foreach (IntVec3 cell in rect)
                {
                    if (cell.InBounds(map))
                    {
                        count += RepairThingsInCell(cell, map);
                    }
                }
                return ApiResult.Ok();
            }
            catch (Exception ex) { return ApiResult.Fail(ex.Message); }
        }

        private int RepairThingsInCell(IntVec3 cell, Map map)
        {
            int repaired = 0;
            // Get all things in cell (Walls, Tables, Turrets, etc)
            List<Thing> things = cell.GetThingList(map);

            foreach (var thing in things)
            {
                // Check if it's a building AND has health
                if (thing.def.useHitPoints && thing.HitPoints < thing.MaxHitPoints)
                {
                    thing.HitPoints = thing.MaxHitPoints;
                    repaired++;
                }
            }
            return repaired;
        }

        public ApiResult SpawnDropPod(SpawnDropPodRequestDto request)
        {
            try
            {
                var map = MapHelper.GetMapByID(request.MapId);
                if (map == null) return ApiResult.Fail(MapLookup.MapNotFound(request.MapId));

                IntVec3 pos = new IntVec3(request.Position.X, 0, request.Position.Z);
                if (!pos.InBounds(map)) return ApiResult.Fail("Position out of bounds.");

                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    try
                    {
                        ActiveTransporterInfo podInfo = new ActiveTransporterInfo();
                        podInfo.openDelay = request.OpenDelay ? 110 : 0;
                        podInfo.leaveSlag = true;

                        // 2. Create and Add Items
                        foreach (var itemDto in request.Items)
                        {
                            if (string.IsNullOrEmpty(itemDto.DefName))
                            {
                                LogApi.Warning("[MapService] Skipping item with missing DefName.");
                                continue;
                            }

                            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(itemDto.DefName);
                            if (def == null)
                            {
                                LogApi.Warning($"[MapService] Failed to get item {itemDto.DefName} from DefDatabase. Skipping item.");
                                continue;
                            }

                            Thing thing = ThingMaker.MakeThing(def, null);
                            thing.stackCount = itemDto.StackCount > 0 ? itemDto.StackCount : 1;

                            // FIX: Check if the item actually SUPPORTS quality before setting it
                            var comp = thing.TryGetComp<CompQuality>();
                            if (comp != null)
                            {
                                // Only apply quality if the item has the component (e.g. Gun, Bed)
                                // Ensure itemDto.Quality matches the expected type (int or enum)
                                comp.SetQuality((QualityCategory)itemDto.Quality, ArtGenerationContext.Outsider);
                            }

                            podInfo.innerContainer.TryAdd(thing);
                        }

                        // 3. Resolve Faction
                        Faction faction = Faction.OfPlayer;
                        if (!string.IsNullOrEmpty(request.Faction))
                        {
                            faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(request.Faction)) ?? Faction.OfPlayer;
                        }

                        // 4. Launch
                        DropPodUtility.MakeDropPodAt(pos, map, podInfo);
                    }
                    catch (Exception ex)
                    {
                        LogApi.Error($"[MapService] MainThread Error: {ex}");
                    }
                });

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Drop Pod Error: {ex}");
                return ApiResult.Fail(ex.Message);
            }
        }
    }
}
