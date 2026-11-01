using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMAPI.Services
{
    public class BuilderService : IBuilderService
    {
        public ApiResult<BlueprintDto> CopyArea(CopyAreaRequestDto request)
        {
            try
            {
                var map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult<BlueprintDto>.Fail(mapError);

                // Normalize Coordinates (ensure min/max are correct regardless of A/B order)
                int minX = Mathf.Min(request.PointA.X, request.PointB.X);
                int minZ = Mathf.Min(request.PointA.Z, request.PointB.Z);
                int maxX = Mathf.Max(request.PointA.X, request.PointB.X);
                int maxZ = Mathf.Max(request.PointA.Z, request.PointB.Z);

                var blueprint = new BlueprintDto
                {
                    Width = (maxX - minX) + 1,
                    Height = (maxZ - minZ) + 1
                };

                // Track added things to avoid duplicates for multi-tile buildings (like Geothermal Generators)
                HashSet<Thing> addedThings = new HashSet<Thing>();

                CellRect rect = new CellRect(minX, minZ, blueprint.Width, blueprint.Height);

                // Iterate every cell
                foreach (IntVec3 cell in rect)
                {
                    if (!cell.InBounds(map)) continue;

                    // 1. Save Terrain (Floor)
                    TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
                    if (terrain != null && terrain.Removable) // Only copy constructed floors, not soil/sand
                    {
                        blueprint.Floors.Add(new SavedTerrainDto
                        {
                            DefName = terrain.defName,
                            RelX = cell.x - minX,
                            RelZ = cell.z - minZ
                        });
                    }

                    // 2. Save Buildings
                    List<Thing> things = cell.GetThingList(map);
                    foreach (var thing in things)
                    {
                        // We only want Buildings, created by players, that we haven't added yet
                        if (thing.def.category == ThingCategory.Building &&
                            thing.def.saveCompressible == false && // Skip filth/motes
                            !addedThings.Contains(thing))
                        {
                            // Important: For multi-tile buildings, only add them if their "InteractionCell" or "Position" is inside or near our rect.
                            // To be safe, we just check if it's the first time we see it.
                            addedThings.Add(thing);

                            blueprint.Buildings.Add(new SavedBuildingDto
                            {
                                DefName = thing.def.defName,
                                StuffDefName = thing.Stuff?.defName, // Material (WoodLog, Steel, etc)
                                RelX = thing.Position.x - minX, // Save relative to anchor
                                RelZ = thing.Position.z - minZ,
                                Rotation = thing.Rotation.AsInt
                            });
                        }
                    }
                }

                return ApiResult<BlueprintDto>.Ok(blueprint);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Copy Error: {ex}");
                return ApiResult<BlueprintDto>.Fail(ex.Message);
            }
        }

        public ApiResult PasteArea(PasteAreaRequestDto request)
        {
            try
            {
                if (request.Blueprint == null) return ApiResult.Fail("Blueprint is null");

                var map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult.Fail(mapError);

                int anchorX = request.Position.X;
                int anchorZ = request.Position.Z;

                // 1. Paste Floors
                foreach (var floorDto in request.Blueprint.Floors)
                {
                    IntVec3 pos = new IntVec3(anchorX + floorDto.RelX, 0, anchorZ + floorDto.RelZ);
                    if (pos.InBounds(map))
                    {
                        var terrainDef = DefDatabase<TerrainDef>.GetNamedSilentFail(floorDto.DefName);
                        if (terrainDef != null)
                        {
                            map.terrainGrid.SetTerrain(pos, terrainDef);
                        }
                    }
                }

                // 2. Paste Buildings
                foreach (var buildDto in request.Blueprint.Buildings)
                {
                    IntVec3 pos = new IntVec3(anchorX + buildDto.RelX, 0, anchorZ + buildDto.RelZ);

                    if (!pos.InBounds(map)) continue;

                    // Resolve Definitions
                    ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(buildDto.DefName);
                    if (thingDef == null) continue;

                    ThingDef stuffDef = null;
                    if (!string.IsNullOrEmpty(buildDto.StuffDefName))
                    {
                        stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(buildDto.StuffDefName);
                    }

                    // Optional: Clear obstacles in the spot before spawning
                    if (request.ClearObstacles)
                    {
                        var obstacles = pos.GetThingList(map).ToList(); // Copy list
                        foreach (var obs in obstacles)
                        {
                            // Destroy items/buildings in the way. Don't destroy pawns.
                            if (obs.def.category == ThingCategory.Building || obs.def.category == ThingCategory.Item || obs.def.category == ThingCategory.Plant)
                            {
                                obs.Destroy();
                            }
                        }
                    }

                    // Create & Spawn
                    Thing thing = ThingMaker.MakeThing(thingDef, stuffDef);
                    thing.Rotation = new Rot4(buildDto.Rotation);

                    // Force the faction to be the player's
                    if (thing.def.CanHaveFaction)
                    {
                        thing.SetFaction(Faction.OfPlayer);
                    }

                    GenSpawn.Spawn(thing, pos, map, thing.Rotation);
                }

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Paste Error: {ex}");
                return ApiResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// One line for a failure inside a single cell: the exception's type and
        /// message plus the method that threw it, which is what names the null
        /// field a bare "Object reference not set" hides (fox ticket #3178).
        /// </summary>
        private static string Describe(string step, Exception ex)
        {
            string where = ex.TargetSite != null
                ? $" in {ex.TargetSite.DeclaringType?.Name}.{ex.TargetSite.Name}"
                : "";
            return $"Exception during {step}: {ex.GetType().Name}: {ex.Message}{where}";
        }

        /// <summary>
        /// A def the architect menu does not offer has no blueprint def at all:
        /// ThingDefGenerator_Buildings creates one only for a def with a
        /// designationCategory, and GenConstruct.PlaceBlueprintForBuild hands
        /// that null to ThingMaker.MakeThing, which is the
        /// NullReferenceException a sculpture raised (#3260; a colony makes one
        /// at a sculpting table instead of placing it). Refusing here names the
        /// route that works instead of a null.
        /// </summary>
        private static string NoBlueprintReason(BuildableDef def)
        {
            var users = (def as ThingDef)?.recipeMaker?.recipeUsers;
            string where = users != null && users.Count > 0
                ? $"the game makes it at {string.Join(" or ", users.Where(u => u != null).Select(u => u.label ?? u.defName).Distinct())} (add a bill there)"
                : "the game does not offer it in the architect menu";
            return $"{def.defName} has no build blueprint: {where}, so it cannot be placed";
        }

        public ApiResult<PlaceResultDto> PlaceBlueprints(PasteAreaRequestDto request)
        {            try
            {
                if (request.Blueprint == null) return ApiResult<PlaceResultDto>.Fail("Blueprint is null");

                var map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult<PlaceResultDto>.Fail(mapError);

                int anchorX = request.Position.X;
                int anchorZ = request.Position.Z;
                var result = new PlaceResultDto();

                // Every cell is asked first and only a cell the game accepts gets
                // a blueprint. PlaceBlueprintForBuild itself does not check, so a
                // marsh cell, a cell holding a wall or a cell outside the map
                // used to be dropped without a word (fox ticket #3089). The
                // source says which step refused, because a caller that adds a
                // ground hint to every refusal sends the reader at the wrong
                // thing when the def has no blueprint at all (fox ticket #3264).
                void Refuse(BuildableDef buildable, IntVec3 pos, string reason, string source)
                {
                    result.Dropped.Add(new PlaceRefusalDto
                    {
                        DefName = buildable.defName,
                        X = pos.x,
                        Z = pos.z,
                        Reason = reason,
                        Source = source,
                    });
                }

                // One cell's own failure must not sink the whole call. The stock
                // code let a NullReferenceException out of a single def (a
                // sculpture, fox ticket #3178) turn into an HTTP 500 that named
                // neither the def nor the cell, so the caller could not even
                // tell what had gone wrong. Each cell now answers on its own,
                // and a thrown exception arrives as a refusal carrying its type,
                // message and the method that threw, which is the read that
                // names the null field.
                void TryPlace(BuildableDef buildable, IntVec3 pos, Rot4 rot, ThingDef stuffDef)
                {
                    AcceptanceReport report;
                    try
                    {
                        report = GenConstruct.CanPlaceBlueprintAt(buildable, pos, rot, map, false, null, null, stuffDef);
                    }
                    catch (Exception ex)
                    {
                        Refuse(buildable, pos, Describe("the placement check", ex), "exception");
                        return;
                    }
                    if (!report.Accepted)
                    {
                        Refuse(buildable, pos, report.Reason, "placement");
                        return;
                    }

                    try
                    {
                        GenConstruct.PlaceBlueprintForBuild(buildable, pos, map, rot, Faction.OfPlayer, stuffDef);
                        result.Placed++;
                    }
                    catch (Exception ex)
                    {
                        Refuse(buildable, pos, Describe("placing the blueprint", ex), "exception");
                    }
                }

                // 1. Place Floor Blueprints
                foreach (var floorDto in request.Blueprint.Floors)
                {
                    IntVec3 pos = new IntVec3(anchorX + floorDto.RelX, 0, anchorZ + floorDto.RelZ);
                    if (!pos.InBounds(map))
                    {
                        result.Dropped.Add(new PlaceRefusalDto
                        {
                            DefName = floorDto.DefName,
                            X = pos.x,
                            Z = pos.z,
                            Reason = "OutOfBounds",
                            Source = "bounds",
                        });
                        continue;
                    }

                    TerrainDef terrainDef = DefDatabase<TerrainDef>.GetNamedSilentFail(floorDto.DefName);
                    if (terrainDef == null)
                    {
                        result.Dropped.Add(new PlaceRefusalDto
                        {
                            DefName = floorDto.DefName,
                            X = pos.x,
                            Z = pos.z,
                            Reason = "UnknownTerrainDef",
                            Source = "unknown_def",
                        });
                        continue;
                    }

                    if (terrainDef.blueprintDef == null)
                    {
                        Refuse(terrainDef, pos, NoBlueprintReason(terrainDef), "no_blueprint");
                        continue;
                    }

                    // PlaceBlueprintForBuild works for TerrainDefs too
                    TryPlace(terrainDef, pos, Rot4.North, null);
                }

                // 2. Place Building Blueprints
                foreach (var buildDto in request.Blueprint.Buildings)
                {
                    IntVec3 pos = new IntVec3(anchorX + buildDto.RelX, 0, anchorZ + buildDto.RelZ);

                    if (!pos.InBounds(map))
                    {
                        result.Dropped.Add(new PlaceRefusalDto
                        {
                            DefName = buildDto.DefName,
                            X = pos.x,
                            Z = pos.z,
                            Reason = "OutOfBounds",
                            Source = "bounds",
                        });
                        continue;
                    }

                    // Resolve Definitions
                    ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(buildDto.DefName);
                    if (thingDef == null)
                    {
                        result.Dropped.Add(new PlaceRefusalDto
                        {
                            DefName = buildDto.DefName,
                            X = pos.x,
                            Z = pos.z,
                            Reason = "UnknownThingDef",
                            Source = "unknown_def",
                        });
                        continue;
                    }

                    ThingDef stuffDef = null;
                    if (!string.IsNullOrEmpty(buildDto.StuffDefName))
                    {
                        stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(buildDto.StuffDefName);
                    }

                    if (thingDef.blueprintDef == null)
                    {
                        Refuse(thingDef, pos, NoBlueprintReason(thingDef), "no_blueprint");
                        continue;
                    }

                    TryPlace(thingDef, pos, new Rot4(buildDto.Rotation), stuffDef);
                }

                return ApiResult<PlaceResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Blueprint Error: {ex}");
                return ApiResult<PlaceResultDto>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Cancel every blueprint and frame inside a rectangle, the game's own
        /// cancel designator (DestroyMode.Cancel) applied cell by cell (fox
        /// ticket #3133). A blueprint that can never be built used to be
        /// permanent: overwriting it with another material worked per cell, and
        /// a cell planned by mistake could not be cleared at all.
        /// </summary>
        public ApiResult<CancelResultDto> CancelBlueprints(CancelBlueprintsRequestDto request)
        {
            try
            {
                var map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult<CancelResultDto>.Fail(mapError);
                if (request.PointA == null || request.PointB == null)
                {
                    return ApiResult<CancelResultDto>.Fail("PointA and PointB are required.");
                }

                int minX = Mathf.Min(request.PointA.X, request.PointB.X);
                int minZ = Mathf.Min(request.PointA.Z, request.PointB.Z);
                int maxX = Mathf.Max(request.PointA.X, request.PointB.X);
                int maxZ = Mathf.Max(request.PointA.Z, request.PointB.Z);

                var result = new CancelResultDto();
                // A multi-cell blueprint sits in several cells of the rectangle;
                // it must be removed once, not once per cell.
                var done = new HashSet<int>();

                var rect = new CellRect(minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
                foreach (var cell in rect)
                {
                    if (!cell.InBounds(map))
                    {
                        result.Skipped.Add(new PlaceRefusalDto
                        {
                            DefName = null,
                            X = cell.x,
                            Z = cell.z,
                            Reason = "OutOfBounds",
                        });
                        continue;
                    }

                    Thing planned = null;
                    foreach (var thing in cell.GetThingList(map))
                    {
                        if (thing is Blueprint || thing is Frame)
                        {
                            planned = thing;
                            break;
                        }
                    }

                    if (planned == null)
                    {
                        result.Skipped.Add(new PlaceRefusalDto
                        {
                            DefName = null,
                            X = cell.x,
                            Z = cell.z,
                            Reason = "NothingPlanned",
                        });
                        continue;
                    }

                    if (!done.Add(planned.thingIDNumber))
                    {
                        continue;
                    }

                    var blueprint = planned as Blueprint;
                    var frame = planned as Frame;
                    var defName = blueprint != null
                        ? blueprint.EntityToBuild()?.defName
                        : frame?.BuildDef?.defName;
                    var stuff = blueprint != null
                        ? (blueprint as Blueprint_Build)?.stuffToUse?.defName
                        : frame?.EntityToBuildStuff()?.defName;
                    var kind = frame != null ? "frame" : "blueprint";
                    int x = planned.Position.x;
                    int z = planned.Position.z;

                    planned.Destroy(DestroyMode.Cancel);

                    result.Cancelled++;
                    result.Removed.Add(new CancelEntryDto
                    {
                        DefName = defName,
                        Stuff = stuff,
                        Kind = kind,
                        X = x,
                        Z = z,
                    });
                }

                return ApiResult<CancelResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Cancel Error: {ex}");
                return ApiResult<CancelResultDto>.Fail(ex.Message);
            }
        }

        public ApiResult<CheckZoneResultDto> CheckZone(CheckZoneRequestDto request)
        {
            try
            {
                var map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult<CheckZoneResultDto>.Fail(mapError);

                if (request.PointA == null || request.PointB == null)
                    return ApiResult<CheckZoneResultDto>.Fail("PointA and PointB are required.");

                int minX = Mathf.Min(request.PointA.X, request.PointB.X);
                int minZ = Mathf.Min(request.PointA.Z, request.PointB.Z);
                int maxX = Mathf.Max(request.PointA.X, request.PointB.X);
                int maxZ = Mathf.Max(request.PointA.Z, request.PointB.Z);

                var issues = new CheckZoneIssuesDto();
                HashSet<Thing> processedThings = new HashSet<Thing>();

                CellRect rect = new CellRect(minX, minZ, (maxX - minX) + 1, (maxZ - minZ) + 1);

                foreach (IntVec3 cell in rect)
                {
                    if (!cell.InBounds(map)) continue;

                    // 1. Check Terrain (affordances)
                    TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
                    if (terrain != null)
                    {
                        bool hasHeavyAffordance = terrain.affordances != null && 
                            terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy);
                        if (!hasHeavyAffordance)
                        {
                            issues.Terrain.Add(new CheckZoneIssueDto
                            {
                                X = cell.x,
                                Z = cell.z,
                                DefName = terrain.defName,
                                Label = terrain.label
                            });
                        }
                    }

                    // 2. Check Things (Ores and Buildings)
                    List<Thing> things = cell.GetThingList(map);
                    foreach (var thing in things)
                    {
                        if (processedThings.Contains(thing)) continue;

                        // Check for mineable ores
                        if (thing.def.mineable)
                        {
                            processedThings.Add(thing);
                            issues.Ores.Add(new CheckZoneIssueDto
                            {
                                X = thing.Position.x,
                                Z = thing.Position.z,
                                DefName = thing.def.defName,
                                Label = thing.def.label
                            });
                        }
                        // Check for buildings
                        else if (thing.def.category == ThingCategory.Building)
                        {
                            processedThings.Add(thing);
                            issues.Buildings.Add(new CheckZoneIssueDto
                            {
                                X = thing.Position.x,
                                Z = thing.Position.z,
                                DefName = thing.def.defName,
                                Label = thing.def.label
                            });
                        }
                    }
                }

                // 3. Check Zones (Growing and Stockpile)
                foreach (Zone zone in map.zoneManager.AllZones)
                {
                    if (zone is Zone_Growing || zone is Zone_Stockpile)
                    {
                        foreach (IntVec3 cell in zone.cells)
                        {
                            if (cell.x >= minX && cell.x <= maxX && cell.z >= minZ && cell.z <= maxZ)
                            {
                                issues.Zones.Add(new CheckZoneIssueDto
                                {
                                    X = cell.x,
                                    Z = cell.z,
                                    DefName = zone.GetType().Name,
                                    Label = zone.label,
                                    ZoneType = zone is Zone_Growing ? "Growing" : "Stockpile"
                                });
                                break;
                            }
                        }
                    }
                }

                bool canBuild = issues.Terrain.Count == 0 && 
                                issues.Ores.Count == 0 && 
                                issues.Buildings.Count == 0 && 
                                issues.Zones.Count == 0;

                return ApiResult<CheckZoneResultDto>.Ok(new CheckZoneResultDto
                {
                    CanBuild = canBuild,
                    Issues = issues
                });
            }
            catch (Exception ex)
            {
                LogApi.Error($"CheckZone Error: {ex}");
                return ApiResult<CheckZoneResultDto>.Fail(ex.Message);
            }
        }
    }
}