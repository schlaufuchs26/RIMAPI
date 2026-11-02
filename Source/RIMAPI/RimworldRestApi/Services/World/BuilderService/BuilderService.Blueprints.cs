using System;
using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    /// <summary>
    /// Placing a layout of blueprints, split out of BuilderService.cs when that
    /// file crossed the 600-line cap (the gate in ServiceFileSizeTests, ticket
    /// #3809). The paste and cancel routes stay with the rest of the service.
    /// </summary>
    public partial class BuilderService
    {
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
                // The game's own build designator wipes what the plan may be
                // built over before it places the blueprint
                // (Designator_Build.DesignateSingleCell): a cooler goes into a
                // wall, and the wall goes with it. Placing the plan without
                // that step left the wall standing on the cell, so the game
                // offered only a deconstruct there and the cooler was never
                // built (fox ticket #4033). What goes is named, because a
                // wall a run did not ask to lose must be readable.
                void WipeBlockers(BuildableDef buildable, IntVec3 pos, Rot4 rot)
                {
                    foreach (Thing onCell in pos.GetThingList(map).ToList())
                    {
                        if (onCell is Frame frame
                            && buildable.blueprintDef != null
                            && buildable.blueprintDef.replaceTags.NotNullAndContainsAnyElement(frame.def.replaceTags))
                        {
                            Wiped(frame, pos);
                            frame.Destroy(DestroyMode.Cancel);
                        }
                    }
                    if (buildable.blueprintDef == null)
                    {
                        return;
                    }
                    foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(pos, rot, buildable.Size))
                    {
                        foreach (Thing onCell in cell.GetThingList(map).ToList())
                        {
                            if (GenSpawn.SpawningWipes(buildable.blueprintDef, onCell.def))
                            {
                                Wiped(onCell, cell);
                            }
                        }
                    }
                    GenSpawn.WipeExistingThings(pos, rot, buildable.blueprintDef, map, DestroyMode.Deconstruct);
                }

                void Wiped(Thing thing, IntVec3 pos)
                {
                    result.Wiped.Add(new WipedThingDto
                    {
                        DefName = thing.def?.defName,
                        Label = thing.LabelShort,
                        X = pos.x,
                        Z = pos.z,
                    });
                }

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

                    // A def with no construction work is not a plan: the game's
                    // own build designator spawns it finished, so a sleeping
                    // spot stands there in the same call instead of waiting for
                    // a builder (fox ticket #3940). A floor keeps the blueprint
                    // path: a buildable terrain carries work in this game, and
                    // the foundation and temporary layers the designator
                    // handles have no counterpart here.
                    if (buildable is ThingDef thingToBuild && BlueprintPlacement.PlacesFinished(
                        buildable.GetStatValueAbstract(StatDefOf.WorkToBuild, stuffDef)))
                    {
                        try
                        {
                            Thing thing = ThingMaker.MakeThing(thingToBuild, stuffDef);
                            thing.SetFactionDirect(Faction.OfPlayer);
                            GenSpawn.Spawn(thing, pos, map, rot);
                            result.Placed++;
                            result.Instantly++;
                        }
                        catch (Exception ex)
                        {
                            Refuse(buildable, pos, Describe("placing the building", ex), "exception");
                        }
                        return;
                    }

                    try
                    {
                        WipeBlockers(buildable, pos, rot);
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
    }
}
