using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    /// <summary>
    /// The shuttle op (fox ticket #3935): put a shuttle on the map and name the
    /// pawns it waits for. A hospitality quest builds that state only after 5 to
    /// 25 in-game days (its QuestNode_ShuttleDelay), and a dev ticks call runs
    /// 20,000 ticks, so no check reached a landed quest shuttle and the boarding
    /// order of ticket #3862 stayed unverified. One call here builds the state a
    /// boarding, quest or shuttle read needs.
    ///
    /// The shuttle is the game's own Shuttle thing: ThingMaker plus GenSpawn run
    /// its PostSpawnSetup, which builds the transport ship a quest shuttle also
    /// carries, so the read and the boarding order see the same object a quest
    /// spawns.
    /// </summary>
    public partial class DevActionService
    {
        /// <summary>The defName of the shuttle the game's quests land on a map.</summary>
        private const string ShuttleDefName = "Shuttle";

        private DevActionResultDto SpawnShuttle(Map map, DevActionRequest request)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ShuttleDefName);
            if (def == null)
            {
                throw new DevRefusal(
                    "this game has no \"" + ShuttleDefName + "\" thing: the shuttle comes with Royalty, "
                        + "and a game without that expansion cannot land one"
                );
            }

            Pawn required = request.PawnId.HasValue ? Pawn(request.PawnId) : null;
            if (required != null && required.MapHeld != map)
            {
                throw new DevRefusal(
                    required.LabelShortCap
                        + " is not on this map ("
                        + (required.MapHeld == null ? "off the map" : "on another map")
                        + "), so a shuttle here could never wait for him; the colonists read prints the ids"
                );
            }

            IntVec3 cell = ShuttleCell(map, def, Cell(map, request));
            Thing shuttle = ThingMaker.MakeThing(def, null);
            GenSpawn.Spawn(shuttle, cell, map, Rot4.North);

            CompShuttle comp = shuttle.TryGetComp<CompShuttle>();
            if (comp == null)
            {
                shuttle.Destroy();
                throw new DevRefusal(
                    def.defName + " carries no shuttle component, so nothing could board it"
                );
            }
            if (required != null)
            {
                comp.requiredPawns.AddUnique(required);
            }
            if (request.Count.HasValue)
            {
                comp.requiredColonistCount = request.Count.Value;
            }

            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Thing = DescribeThing(shuttle),
            };
            result.Detail =
                "landed the "
                + def.label
                + " at ("
                + cell.x
                + ","
                + cell.z
                + "), id "
                + shuttle.thingIDNumber
                + (required == null
                    ? "; it waits for no particular pawn"
                    : "; it waits for "
                        + required.LabelShortCap
                        + " (id "
                        + required.thingIDNumber
                        + "), who stands "
                        + required.Position.DistanceTo(cell).ToString("0")
                        + " cells away")
                + (comp.requiredColonistCount > 0
                    ? " and for " + comp.requiredColonistCount + " of the colony's people"
                    : "");
            return result;
        }

        /// <summary>
        /// The cell a shuttle lands on: the request's, else a colonist's, and
        /// near that one the first spot whose cells are free and take the
        /// shuttle's terrain affordance. A 5x3 building dropped on a wall would
        /// overlap it, so the search matters more here than for a single item.
        /// </summary>
        private static IntVec3 ShuttleCell(Map map, ThingDef def, IntVec3 near)
        {
            IntVec2 size = def.size;
            for (int radius = 0; radius <= 40; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(near, radius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    CellRect rect = new CellRect(cell.x, cell.z, size.x, size.z);
                    if (!rect.Cells.All(c => CanLandOn(map, def, c))) continue;
                    return cell;
                }
            }
            throw new DevRefusal(
                "no free "
                    + size.x
                    + "x"
                    + size.z
                    + " spot near ("
                    + near.x
                    + ","
                    + near.z
                    + ") to land the "
                    + def.label
                    + " on; name a cell with x and z"
            );
        }

        /// <summary>True for a cell a shuttle may stand on: no building, no fog, its own terrain.</summary>
        private static bool CanLandOn(Map map, ThingDef def, IntVec3 cell)
        {
            if (!cell.InBounds(map) || cell.Fogged(map)) return false;
            if (cell.GetEdifice(map) != null) return false;
            // A pawn under the hull would be trapped in a 5x3 impassable
            // building; the search starts at the target cell, which is usually
            // a colonist's own, so this check is what moves the shuttle beside
            // him instead of onto him.
            if (cell.GetFirstPawn(map) != null) return false;
            if (cell.GetThingList(map).Any(t => t.def.passability == Traversability.Impassable)) return false;
            TerrainDef terrain = cell.GetTerrain(map);
            return def.terrainAffordanceNeeded == null
                || terrain.affordances.Contains(def.terrainAffordanceNeeded);
        }
    }
}
