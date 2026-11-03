using System.Collections.Generic;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The floor orders the Architect tab offers (fox ticket #4316, parity wave
    /// 7): Designator_SmoothSurface (smooth a wall or a stone floor) and
    /// Designator_RemoveFloor. Both are cell designators a player drags over the
    /// map, so the route takes a rectangle and applies the game's own
    /// per-cell checks (FloorWorkRules) before it calls the game's own
    /// designation helpers.
    /// </summary>
    public static class FloorWork
    {
        public const string Smooth = "smooth";
        public const string RemoveFloor = "remove-floor";

        // How many refusals an answer carries: a rectangle over a whole colony
        // could refuse thousands of cells, and the count plus a sample is what
        // a caller acts on.
        private const int MaxRefusals = 50;

        public static bool IsKnownType(string type)
        {
            string t = (type ?? string.Empty).Trim().ToLowerInvariant();
            return t == Smooth || t == RemoveFloor;
        }

        public static FloorWorkResultDto Run(Map map, string type, CellRect rect)
        {
            string kind = (type ?? string.Empty).Trim().ToLowerInvariant();
            var result = new FloorWorkResultDto
            {
                MapId = map.uniqueID,
                Type = kind,
                Refusals = new List<FloorRefusalDto>(),
            };

            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    result.Cells++;
                    result.Unseen++;
                    continue;
                }

                result.Cells++;
                if (kind == Smooth)
                {
                    RunSmoothCell(map, cell, result);
                }
                else
                {
                    RunRemoveFloorCell(map, cell, result);
                }
            }

            result.RefusalsTruncated = result.Refused > result.Refusals.Count;
            return result;
        }

        private static void RunSmoothCell(Map map, IntVec3 cell, FloorWorkResultDto result)
        {
            Building edifice = cell.GetEdifice(map);
            var facts = new FloorWorkRules.SmoothCell
            {
                InBounds = true,
                Fogged = cell.Fogged(map),
                TooCloseToMapEdge = cell.InNoBuildEdgeArea(map),
                AlreadyDesignated =
                    map.designationManager.DesignationAt(cell, DesignationDefOf.SmoothFloor) != null
                    || map.designationManager.DesignationAt(cell, DesignationDefOf.SmoothWall) != null,
                EdificeSmoothable = edifice != null && edifice.def.IsSmoothable,
                EdificeBlocksFloor =
                    edifice != null && !SmoothSurfaceDesignatorUtility.CanSmoothFloorUnder(edifice),
                HasSmoothableStone = cell
                    .GetAffordances(map)
                    .Contains(TerrainAffordanceDefOf.SmoothableStone),
            };

            string reason;
            switch (FloorWorkRules.Smooth(facts, out reason))
            {
                case FloorWorkRules.Verdict.DesignateWall:
                    if (SmoothSurfaceDesignatorUtility.DesignateSmoothWall(map, cell))
                    {
                        result.Designated++;
                        result.SmoothWall++;
                    }
                    else
                    {
                        Refuse(result, cell, "the wall on this cell is not smoothable");
                    }
                    return;
                case FloorWorkRules.Verdict.Designate:
                    SmoothSurfaceDesignatorUtility.DesignateSmoothFloor(map, cell);
                    result.Designated++;
                    result.SmoothFloor++;
                    return;
                case FloorWorkRules.Verdict.Already:
                    result.Already++;
                    return;
                case FloorWorkRules.Verdict.Unseen:
                    result.Unseen++;
                    return;
                default:
                    Refuse(result, cell, reason);
                    return;
            }
        }

        private static void RunRemoveFloorCell(Map map, IntVec3 cell, FloorWorkResultDto result)
        {
            Building edifice = cell.GetEdifice(map);
            var facts = new FloorWorkRules.RemoveFloorCell
            {
                InBounds = true,
                Fogged = cell.Fogged(map),
                AlreadyDesignated =
                    map.designationManager.DesignationAt(cell, DesignationDefOf.RemoveFloor) != null,
                EdificeBlocksFloor =
                    edifice != null
                    && edifice.def.Fillage == FillCategory.Full
                    && edifice.def.passability == Traversability.Impassable,
                TerrainRemovable = map.terrainGrid.CanRemoveTopLayerAt(cell),
                BuildingBlocksRemoval = WorkGiver_ConstructRemoveFloor.AnyBuildingBlockingFloorRemoval(
                    cell,
                    map
                ),
            };

            string reason;
            switch (FloorWorkRules.RemoveFloor(facts, out reason))
            {
                case FloorWorkRules.Verdict.Designate:
                    map.designationManager.AddDesignation(
                        new Designation(cell, DesignationDefOf.RemoveFloor)
                    );
                    result.Designated++;
                    return;
                case FloorWorkRules.Verdict.Already:
                    result.Already++;
                    return;
                case FloorWorkRules.Verdict.Unseen:
                    result.Unseen++;
                    return;
                default:
                    Refuse(result, cell, reason);
                    return;
            }
        }

        private static void Refuse(FloorWorkResultDto result, IntVec3 cell, string reason)
        {
            result.Refused++;
            if (result.Refusals.Count < MaxRefusals)
            {
                result.Refusals.Add(
                    new FloorRefusalDto
                    {
                        X = cell.x,
                        Z = cell.z,
                        Reason = reason,
                    }
                );
            }
        }
    }
}
