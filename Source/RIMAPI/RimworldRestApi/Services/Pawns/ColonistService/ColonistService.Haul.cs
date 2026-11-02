using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    // The haul half of the colonist service: one named thing into one named
    // destination, which is how a run moves a thing without a screen click
    // (fox ticket #3136). Split out of ColonistService.cs when the cell choice
    // for an ordered haul arrived (ticket #4058), so that file stays under the
    // service size gate.
    public partial class ColonistService : IColonistService
    {
        // Hauling one named thing: the job the game's own haul work giver would
        // queue, but for the thing and the destination the caller names. A
        // destination is any haul destination, not only a Building_Storage: a
        // grave is a Building_CorpseCasket whose filter takes corpses, so the
        // burial of a colonist needs exactly that (ticket #3150). Without a
        // destination a corpse goes into a free grave before it goes back into
        // a stockpile, because that is what clears the "left unburied" thought;
        // everything else takes the game's own best store. A corpse that stayed
        // unburied for six days while Hauling ran at 1 had no lever at all
        // (ticket #3136).
        public ApiResult MakeJobHaul(int mapId, int pawnId, int itemId, int destId)
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null)
                {
                    return ApiResult.Fail(mapError);
                }

                Thing thing = map
                    .listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == itemId);
                if (thing == null)
                {
                    // The stored read lists a container's contents too, so an id
                    // from it can name a corpse that sits in a grave already or
                    // the wood delivered into a frame. Those things are despawned
                    // and no haul can take them; say which container holds the
                    // thing instead of answering "not found" (ticket #3150).
                    Thing holder = ThingHolderHelper.FindContainerHolding(map, itemId);
                    if (holder != null)
                    {
                        return ApiResult.Fail(
                            ThingHolderHelper.DescribeHeld(itemId, holder)
                                + "; no haul can take it, and a corpse that sits in a grave is buried already"
                        );
                    }
                    return ApiResult.Fail($"Thing with ID={itemId} not found");
                }
                if (!thing.Spawned)
                {
                    return ApiResult.Fail(
                        $"Thing {itemId} ({thing.def.defName}) is not on the map"
                    );
                }

                Pawn pawn;
                if (pawnId > 0)
                {
                    pawn = PawnHelper.FindPawnForOrder(map, pawnId);
                    if (pawn == null)
                    {
                        return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                    }
                    // A carried colonist can haul nothing, and the old lookup
                    // answered "not found" for him (fox #3719).
                    string carried = CombatRefusal.ActorRefusal(
                        Describe(pawn), "haul", PawnHelper.RefusalStateOf(pawn));
                    if (carried != null) return ApiResult.Fail(carried);
                }
                else
                {
                    pawn = map.mapPawns.FreeColonists.FirstOrDefault(p =>
                        !p.Downed && !p.Dead && p.jobs != null && !p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)
                    );
                    if (pawn == null)
                    {
                        return ApiResult.Fail(
                            "No colonist can haul right now: none is up and able to do the Hauling work"
                        );
                    }
                }
                if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                {
                    return ApiResult.Fail(
                        $"Pawn {pawn.thingIDNumber} cannot haul: the Hauling work type is switched off or the pawn is incapable"
                    );
                }

                Job job;
                if (destId > 0)
                {
                    Thing dest = map.listerThings.AllThings.FirstOrDefault(t =>
                        t.thingIDNumber == destId
                    );
                    if (dest == null)
                    {
                        return ApiResult.Fail($"Destination with ID={destId} not found");
                    }
                    string refusal;
                    job = HaulJobToDestination(pawn, thing, dest, out refusal);
                    if (job == null)
                    {
                        return ApiResult.Fail(refusal);
                    }
                }
                else
                {
                    job = BestHaulJob(pawn, thing);
                    if (job == null)
                    {
                        return ApiResult.Fail(
                            $"No storage on the map accepts {thing.LabelShort} ({thing.def.defName}), so the game has nowhere to haul it"
                        );
                    }
                }

                // playerForced is what makes the haul go through for a thing a
                // colonist would otherwise leave alone: a forbidden corpse, or a
                // store the colony would not pick by itself.
                job.playerForced = true;
                if (!pawn.jobs.TryTakeOrderedJob(job))
                {
                    return ApiResult.Fail($"Failed to assign job to pawn: {pawn.thingIDNumber}");
                }
                // A container job names the destination it went into, which is
                // how a caller that left dest_id open learns which grave the
                // mod picked (ticket #3150).
                int chosenDest = destId;
                if (job.def == JobDefOf.HaulToContainer && job.targetB.HasThing)
                {
                    chosenDest = job.targetB.Thing.thingIDNumber;
                }
                return ApiResult.Ok(
                    new
                    {
                        pawn_id = pawn.thingIDNumber,
                        pawn_label = pawn.LabelShort,
                        item_id = thing.thingIDNumber,
                        def_name = thing.def.defName,
                        label = thing.LabelShort,
                        dest_id = chosenDest,
                        job = job.def.defName,
                    }
                );
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // HaulJobToDestination builds the job into the destination the caller
        // named: a slot group (a stockpile or a shelf) takes the cell job, a
        // container (a grave, a sarcophagus) the container job. It refuses with
        // the reason, because "failed to build the job" leaves the caller
        // guessing which of the checks said no (ticket #3150).
        private static Job HaulJobToDestination(Pawn pawn, Thing thing, Thing dest, out string refusal)
        {
            refusal = null;
            IHaulDestination destination = dest as IHaulDestination;
            if (destination == null)
            {
                refusal =
                    $"Thing {dest.thingIDNumber} is {dest.def.defName}, not a haul destination; a destination is a grave, a shelf, a stockpile or another building that holds things";
                return null;
            }
            if (!destination.HaulDestinationEnabled)
            {
                refusal = $"{dest.LabelShort} (#{dest.thingIDNumber}) is switched off as a haul destination";
                return null;
            }
            if (!destination.Accepts(thing))
            {
                // A casket names which of its checks said no (fox ticket
                // #3784): the corpse already inside, the colonist the grave
                // is assigned to, or its filter. The general sentence stays
                // for a destination whose own facts explain nothing.
                Building_CorpseCasket casket = dest as Building_CorpseCasket;
                string cause =
                    casket != null
                        ? CasketRefusal.Cause(
                            CasketFacts(casket),
                            thing.LabelShort,
                            thing is Corpse,
                            (thing as Corpse)?.InnerPawn?.LabelShort ?? ""
                        )
                        : null;
                refusal =
                    cause
                    ?? $"{dest.LabelShort} does not accept {thing.LabelShort}: the storage filter, the grave's assignment or the item's own settings say no";
                return null;
            }
            try
            {
                if (destination is ISlotGroupParent slotGroupParent)
                {
                    IntVec3 cell = FirstStoreCell(pawn, thing, slotGroupParent.GetSlotGroup());
                    if (!cell.IsValid)
                    {
                        refusal = $"{dest.LabelShort} (#{dest.thingIDNumber}) has no free cell that takes {thing.LabelShort}";
                        return null;
                    }
                    return HaulAIUtility.HaulToCellStorageJob(pawn, thing, cell, fitInStoreCell: false);
                }
                Job job = HaulAIUtility.HaulToContainerJob(pawn, thing, dest);
                if (job == null)
                {
                    refusal = $"{dest.LabelShort} (#{dest.thingIDNumber}) cannot hold things, so there is no container job for it";
                }
                return job;
            }
            catch (Exception ex)
            {
                refusal =
                    $"Failed to build the haul job for thing {thing.thingIDNumber} into {dest.thingIDNumber}: {ex.GetType().Name}: {ex.Message}";
                return null;
            }
        }

        // CasketFacts is what the refusal needs from a grave: its name and id,
        // the corpse inside, the colonist it is assigned to and its filter
        // (fox ticket #3784).
        private static CasketRefusal.Casket CasketFacts(Building_CorpseCasket casket)
        {
            Building_Grave grave = casket as Building_Grave;
            return new CasketRefusal.Casket
            {
                Label = casket.LabelShort,
                Id = casket.thingIDNumber,
                Holds = casket.Corpse?.LabelShort ?? "",
                AssignedPawn = grave?.AssignedPawn?.LabelShort ?? "",
                Filter = casket.GetStoreSettings()?.filter?.Summary ?? "",
            };
        }

        // FirstStoreCell is the cell of a slot group the pawn would put the
        // thing on; the game's own check, so a cell that is full or holds
        // something else is skipped rather than handed to the job. A cell the
        // pawn's own running job reserved counts as free: the ordered job
        // replaces that job and with it the reservation (ticket #4058).
        private static IntVec3 FirstStoreCell(Pawn pawn, Thing thing, SlotGroup group)
        {
            if (group == null)
            {
                return IntVec3.Invalid;
            }
            List<IntVec3> cells = group.CellsList;
            List<HaulCellChoice.Verdict> verdicts = new List<HaulCellChoice.Verdict>(cells.Count);
            foreach (IntVec3 cell in cells)
            {
                verdicts.Add(StoreCellVerdict(pawn, thing, cell));
            }
            int index = HaulCellChoice.Choose(verdicts);
            return index < 0 ? IntVec3.Invalid : cells[index];
        }

        // StoreCellVerdict says what stands between the pawn and one cell of the
        // destination. StoreUtility.IsGoodStoreCell asks whether the carrier can
        // reserve the cell new, which is false for a cell his own job already
        // reserved; that is the one refusal the order lifts by itself, so the
        // rest of the checks are asked again without the reservation.
        private static HaulCellChoice.Verdict StoreCellVerdict(Pawn pawn, Thing thing, IntVec3 cell)
        {
            if (StoreUtility.IsGoodStoreCell(cell, pawn.Map, thing, pawn, pawn.Faction))
            {
                return HaulCellChoice.Verdict.Good;
            }
            if (!pawn.HasReserved(cell))
            {
                return HaulCellChoice.Verdict.Blocked;
            }
            if (cell.IsForbidden(pawn) || !pawn.CanReach(cell, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return HaulCellChoice.Verdict.Blocked;
            }
            // A carrier-less check still refuses a full cell, a blocker, a fire
            // and a constructible plan on the cell.
            return StoreUtility.IsGoodStoreCell(cell, pawn.Map, thing, null, null)
                ? HaulCellChoice.Verdict.CarrierReserved
                : HaulCellChoice.Verdict.Blocked;
        }

        // BestHaulJob is the job for a haul the caller left open. A corpse goes
        // into a free grave first, because that is what clears the "left
        // unburied" thought; the game's own search alone finds no grave for a
        // corpse that already lies in a stockpile of the same priority, since it
        // only takes a destination that is strictly better (ticket #3150).
        // Everything else, and a corpse with no free grave, takes the game's own
        // best store.
        private static Job BestHaulJob(Pawn pawn, Thing thing)
        {
            StoragePriority current = StoreUtility.CurrentStoragePriorityOf(thing, forced: true);
            Job job = null;
            if (thing is Corpse && thing.CanBeBuried())
            {
                job = ContainerDestinationJob(pawn, thing, current);
            }
            if (job == null)
            {
                try
                {
                    job = HaulAIUtility.HaulToStorageJob(pawn, thing, true);
                }
                catch (Exception)
                {
                    job = null;
                }
            }
            if (job == null)
            {
                job = ContainerDestinationJob(pawn, thing, current);
            }
            return job;
        }

        // ContainerDestinationJob is the job into the nearest free container
        // destination that takes the thing. TryFindBestBetterNonSlotGroupStorageFor
        // is the game's own search; acceptSamePriority is what lets it see a
        // grave next to a stockpile of the same priority, and it still honours
        // the grave's filter, its assignment, reservations and reachability.
        private static Job ContainerDestinationJob(Pawn pawn, Thing thing, StoragePriority current)
        {
            if (
                !StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(
                    thing,
                    pawn,
                    pawn.Map,
                    current,
                    pawn.Faction,
                    out IHaulDestination destination,
                    acceptSamePriority: true
                )
            )
            {
                return null;
            }
            Thing container = destination as Thing;
            if (container == null)
            {
                return null;
            }
            try
            {
                return HaulAIUtility.HaulToContainerJob(pawn, thing, container);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
