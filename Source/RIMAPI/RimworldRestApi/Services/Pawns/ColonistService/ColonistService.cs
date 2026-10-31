using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    public partial class ColonistService : IColonistService
    {
        public ColonistService() { }

        public ApiResult<PawnDto> GetColonist(int pawnId)
        {
            try
            {
                var result = ColonistsHelper.GetSingleColonistDto(pawnId);
                if (result == null)
                {
                    return ApiResult<PawnDto>.Fail($"Failed to find pawn with id: {pawnId}");
                }
                return ApiResult<PawnDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnDto>
                    .Fail($"Failed to find pawn with id: {pawnId} - {ex.Message}");
            }
        }

        public ApiResult<BodyPartsDto> GetColonistBodyParts(int pawnId)
        {
            BodyPartsDto bodyParts = new BodyPartsDto();

            try
            {
                Pawn colonist = PawnsFinder
                    .AllMaps_FreeColonists.Where(p => p.thingIDNumber == pawnId)
                    .FirstOrDefault();

                Material bodyMaterial = colonist.Drawer.renderer.BodyGraphic.MatAt(Rot4.South);
                Texture2D bodyTexture = (Texture2D)bodyMaterial.mainTexture;

                Material headMaterial = colonist.Drawer.renderer.HeadGraphic.MatAt(Rot4.South);
                Texture2D headTexture = (Texture2D)headMaterial.mainTexture;

                bodyParts.BodyImage = TextureHelper.TextureToBase64(bodyTexture);
                bodyParts.BodyColor = bodyMaterial.color.ToString();
                bodyParts.HeadImage = TextureHelper.TextureToBase64(headTexture);
                bodyParts.HeadColor = headMaterial.color.ToString();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting body image - {ex.Message}");
            }
            return ApiResult<BodyPartsDto>.Ok(bodyParts);
        }

        public ApiResult<PawnDetailedRequestDto> GetColonistDetailed(int pawnId)
        {
            try
            {
                var result = ColonistsHelper.GetSingleColonistDetailedDto(pawnId);
                if (result == null)
                {
                    return ApiResult<PawnDetailedRequestDto>.Fail($"Failed to find pawn with id: {pawnId}");
                }
                return ApiResult<PawnDetailedRequestDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnDetailedRequestDto>
                    .Fail($"Failed to find pawn with id: {pawnId} - {ex.Message}");
            }
        }

        public ApiResult<PawnInventoryDto> GetColonistInventory(int pawnId)
        {
            try
            {

                Pawn colonist = ColonistsHelper.GetColonistsList()
                    .Where(p => p.thingIDNumber == pawnId)
                    .FirstOrDefault();

                if (colonist == null) return ApiResult<PawnInventoryDto>
                    .Fail($"Colonist {pawnId} not found.");

                var inventory = PawnHelper.GetPawnInventory(colonist);
                return ApiResult<PawnInventoryDto>.Ok(inventory);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnInventoryDto>.Fail(ex.Message);
            }
        }

        public ApiResult<List<PawnDto>> GetColonists()
        {
            var result = ColonistsHelper.GetColonistsDtos();
            return ApiResult<List<PawnDto>>.Ok(result);
        }

        public ApiResult<List<PawnPositionDto>> GetColonistPositions()
        {
            var result = ColonistsHelper.GetColonistPositions();
            return ApiResult<List<PawnPositionDto>>.Ok(result);
        }

        public ApiResult<List<PawnDetailedRequestDto>> GetColonistsDetailed()
        {
            var result = ColonistsHelper.GetColonistsDetailedDtos();
            return ApiResult<List<PawnDetailedRequestDto>>.Ok(result);
        }

        public ApiResult<OpinionAboutPawnDto> GetOpinionAboutPawn(int pawnId, int otherPawnId)
        {
            Pawn pawn = PawnHelper.FindPawnById(pawnId);
            if (pawn == null)
                return ApiResult<OpinionAboutPawnDto>.Fail($"Failed to find pawn with {pawnId} id");

            Pawn other = PawnHelper.FindPawnById(otherPawnId);
            if (other == null)
                return ApiResult<OpinionAboutPawnDto>.Fail($"Failed to find other pawn with {otherPawnId} id");

            var result = new OpinionAboutPawnDto
            {
                Opinion = pawn.relations.OpinionOf(other),
                OpinionAboutMe = other.relations.OpinionOf(pawn),
            };
            return ApiResult<OpinionAboutPawnDto>.Ok(result);
        }

        public ApiResult<List<OutfitDto>> GetOutfits()
        {
            var result = PawnHelper.GetOutfits();
            return ApiResult<List<OutfitDto>>.Ok(result);
        }

        public ApiResult<ImageDto> GetPawnPortraitImage(
            int pawnId,
            int width,
            int height,
            string direction
        )
        {
            Pawn pawn = PawnHelper.FindPawnById(pawnId);
            if (pawn == null)
            {
                return ApiResult<ImageDto>.Fail($"Pawn with ID {pawnId} not found.");
            }
            var result = TextureHelper.GetPawnPortraitImage(pawn, width, height, direction);
            return ApiResult<ImageDto>.Ok(result);
        }

        public ApiResult<List<TimeAssignmentDto>> GetTimeAssignmentsList()
        {
            var result = DefDatabase<TimeAssignmentDef>
                .AllDefs.Select(s => new TimeAssignmentDto { Name = s.defName })
                .ToList();
            return ApiResult<List<TimeAssignmentDto>>.Ok(result);
        }

        public ApiResult<TraitDefDto> GetTraitDefDto(string traitName)
        {
            TraitDef trait = DefDatabase<TraitDef>.GetNamed(traitName, false);
            var result = DefDatabaseHelper.GetTraitDefDto(trait);
            return ApiResult<TraitDefDto>.Ok(result);
        }

        public ApiResult<WorkListDto> GetWorkList()
        {
            WorkListDto workList = new WorkListDto { Work = new List<string>() };

            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if (workType == null)
                    continue;
                workList.Work.Add(workType.defName);
            }

            return ApiResult<WorkListDto>.Ok(workList);
        }

        // GetColonistWorkPriorities answers "what will this pawn do, and what is
        // it set to" in one read (fox, #3085): every work type with its stored
        // level, the level the game acts on, and whether the pawn can do it at
        // all. Without it a caller has to guess from the work tab.
        public ApiResult<ColonistWorkPrioritiesDto> GetColonistWorkPriorities(int pawnId)
        {
            try
            {
                if (pawnId <= 0)
                {
                    return ApiResult<ColonistWorkPrioritiesDto>.Fail(
                        "pawn_id is required; it is the id the colonists read reports."
                    );
                }

                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                {
                    return ApiResult<ColonistWorkPrioritiesDto>.Fail(
                        $"Could not find pawn with ID {pawnId}"
                    );
                }

                var result = new ColonistWorkPrioritiesDto
                {
                    PawnId = pawn.thingIDNumber,
                    PawnName = pawn.LabelShortCap,
                    ManualPriorities = Find.PlaySettings?.useWorkPriorities ?? false,
                    Initialized = pawn.workSettings != null && pawn.workSettings.EverWork,
                };

                if (!result.Initialized)
                {
                    return ApiResult<ColonistWorkPrioritiesDto>.Ok(result);
                }

                DefMap<WorkTypeDef, int> stored = StoredWorkPriorities(pawn);
                foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefs)
                {
                    if (workType == null)
                        continue;
                    int priority = pawn.workSettings.GetPriority(workType);
                    result.Work.Add(
                        new WorkPriorityEntryDto
                        {
                            WorkType = workType.defName,
                            Priority = priority,
                            Stored = stored != null ? stored[workType] : priority,
                            Disabled = pawn.WorkTypeIsDisabled(workType),
                        }
                    );
                }

                return ApiResult<ColonistWorkPrioritiesDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<ColonistWorkPrioritiesDto>.Fail(ex.Message);
            }
        }

        // StoredWorkPriorities reads the levels the pawn keeps. The game holds
        // them in a private DefMap and GetPriority collapses every active type
        // to 3 while manual priorities are off, so the numbers behind the work
        // tab are only visible through that field. Null means unreadable, and
        // the caller falls back to the effective level.
        private static DefMap<WorkTypeDef, int> StoredWorkPriorities(Pawn pawn)
        {
            try
            {
                return Traverse
                    .Create(pawn.workSettings)
                    .Field("priorities")
                    .GetValue<DefMap<WorkTypeDef, int>>();
            }
            catch (Exception ex)
            {
                Core.LogApi.Error(
                    $"Error reading stored work priorities for pawn {pawn.thingIDNumber} - {ex.Message}"
                );
                return null;
            }
        }

        // Tending is the game's own job: a doctor walks to the patient and treats
        // the wound, with a named medicine when one is given. The tool can queue
        // it but must not invent the outcome, so the checks below are the ones
        // the work giver makes before it hands the job out, and each refusal
        // names why (ticket #3117).
        public ApiResult MakeJobTend(int mapId, int pawnId, int patientId, int medicineId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null)
                {
                    return ApiResult.Fail($"Map with ID={mapId} not found");
                }
                Pawn doctor = map
                    .listerThings.AllThings.OfType<Pawn>()
                    .FirstOrDefault(p => p.thingIDNumber == pawnId);
                if (doctor == null)
                {
                    return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                }
                Pawn patient = map
                    .listerThings.AllThings.OfType<Pawn>()
                    .FirstOrDefault(p => p.thingIDNumber == patientId);
                if (patient == null)
                {
                    return ApiResult.Fail($"Patient with ID={patientId} not found");
                }
                if (patient == doctor)
                {
                    return ApiResult.Fail(
                        $"Pawn {pawnId} cannot be ordered to tend itself; self-tending is a trait, not a job"
                    );
                }
                if (doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
                {
                    return ApiResult.Fail(
                        $"Pawn {pawnId} cannot doctor: the Doctor work type is switched off or the pawn is incapable"
                    );
                }
                if (!patient.health.HasHediffsNeedingTend(false))
                {
                    return ApiResult.Fail(
                        $"Patient {patientId} has nothing to tend: no hediff needs treatment right now"
                    );
                }

                Thing medicine = null;
                if (medicineId > 0)
                {
                    medicine = map.listerThings.AllThings.FirstOrDefault(t =>
                        t.thingIDNumber == medicineId
                    );
                    if (medicine == null)
                    {
                        return ApiResult.Fail($"Medicine with ID={medicineId} not found");
                    }
                    if (!medicine.def.IsMedicine)
                    {
                        return ApiResult.Fail(
                            $"Thing {medicineId} is {medicine.def.defName}, not medicine"
                        );
                    }
                }

                Job job;
                try
                {
                    job = JobMaker.MakeJob(JobDefOf.TendPatient, patient, medicine);
                    job.count = 1;
                }
                catch (Exception ex)
                {
                    return ApiResult.Fail(
                        $"Failed to build the tend job for patient {patientId}: "
                            + $"{ex.GetType().Name}: {ex.Message}"
                    );
                }

                if (!doctor.jobs.TryTakeOrderedJob(job))
                {
                    return ApiResult.Fail($"Failed to assign job to pawn: {doctor.thingIDNumber}");
                }
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

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
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null)
                {
                    return ApiResult.Fail($"Map with ID={mapId} not found");
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
                    Thing holder = FindContainerHolding(map, itemId);
                    if (holder != null)
                    {
                        return ApiResult.Fail(
                            $"Thing {itemId} is inside {holder.def.defName} #{holder.thingIDNumber} ({holder.LabelShort}), not on the map, so no haul can take it; "
                                + "a corpse that sits in a grave is buried already"
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
                    pawn = map
                        .listerThings.AllThings.OfType<Pawn>()
                        .FirstOrDefault(p => p.thingIDNumber == pawnId);
                    if (pawn == null)
                    {
                        return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                    }
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
                refusal =
                    $"{dest.LabelShort} does not accept {thing.LabelShort}: the storage filter, the grave's assignment or the item's own settings say no";
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

        // FirstStoreCell is the cell of a slot group the pawn would put the
        // thing on; the game's own check, so a cell that is full or holds
        // something else is skipped rather than handed to the job.
        private static IntVec3 FirstStoreCell(Pawn pawn, Thing thing, SlotGroup group)
        {
            if (group == null)
            {
                return IntVec3.Invalid;
            }
            foreach (IntVec3 cell in group.CellsList)
            {
                if (StoreUtility.IsGoodStoreCell(cell, pawn.Map, thing, pawn, pawn.Faction))
                {
                    return cell;
                }
            }
            return IntVec3.Invalid;
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

        // FindContainerHolding is the container a despawned thing sits in: a
        // grave's corpse, a frame's delivered wood, a shelf's inner owner. The
        // stored read lists those things, so a write that takes an id from it
        // has to name the container instead of answering "not found".
        private static Thing FindContainerHolding(Map map, int thingId)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (!(building is IThingHolder holder) || building is ISlotGroupParent)
                {
                    continue;
                }
                if (HoldsThing(holder, thingId, 0))
                {
                    return building;
                }
            }
            return null;
        }

        private static bool HoldsThing(IThingHolder holder, int thingId, int depth)
        {
            if (depth > 4)
            {
                return false;
            }
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner == null)
            {
                return false;
            }
            for (int i = 0; i < owner.Count; i++)
            {
                Thing held = owner[i];
                if (held.thingIDNumber == thingId)
                {
                    return true;
                }
                if (held is IThingHolder nested && HoldsThing(nested, thingId, depth + 1))
                {
                    return true;
                }
            }
            return false;
        }

        public ApiResult MakeJobEquip(int mapId, int pawnId, int equipmentId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null)
                {
                    return ApiResult.Fail($"Map with ID={mapId} not found");
                }
                Pawn pawn = map
                    .listerThings.AllThings.OfType<Pawn>()
                    .FirstOrDefault(p => p.thingIDNumber == pawnId);
                if (pawn == null)
                {
                    return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                }

                Thing foundThing = map.listerThings.AllThings.FirstOrDefault(t =>
                    t.thingIDNumber == equipmentId
                );
                if (foundThing == null)
                {
                    return ApiResult.Fail($"Thing with ID={equipmentId} not found");
                }

                // The def decides the job, and only its own check may run: a
                // weapon has no ThingDef.apparel, so ApparelUtility.HasPartsToWear
                // dereferences null there and upstream's blanket catch turned that
                // NullReferenceException into "Failed to determine equip job" for
                // every weapon (#3102). Apparel is worn, everything else that can
                // be held is equipped (beer included, JobDriver_Equip handles it).
                Job job = null;
                try
                {
                    if (foundThing.def.IsApparel)
                    {
                        if (!ApparelUtility.HasPartsToWear(pawn, foundThing.def))
                        {
                            return ApiResult.Fail(
                                $"Pawn {pawnId} has no body part to wear {foundThing.def.defName}"
                            );
                        }
                        job = JobMaker.MakeJob(JobDefOf.Wear, foundThing);
                    }
                    else
                    {
                        if (pawn.equipment == null)
                        {
                            return ApiResult.Fail($"Pawn {pawnId} cannot hold equipment");
                        }
                        string cantReason = null;
                        if (!EquipmentUtility.CanEquip(foundThing, pawn, out cantReason))
                        {
                            return ApiResult.Fail(
                                $"Pawn {pawnId} cannot equip {foundThing.def.defName}: "
                                    + (cantReason ?? "unknown reason")
                            );
                        }
                        job = JobMaker.MakeJob(JobDefOf.Equip, foundThing);
                    }
                }
                catch (Exception ex)
                {
                    return ApiResult.Fail(
                        $"Failed to determine equip job for item {foundThing.def.defName}: "
                            + $"{ex.GetType().Name}: {ex.Message}"
                    );
                }

                bool result = pawn.jobs.TryTakeOrderedJob(job);
                if (!result)
                {
                    return ApiResult.Fail($"Failed to assign job to pawn: {pawn.thingIDNumber}");
                }
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult SetColonistsWorkPriority(ColonistsWorkPrioritiesRequestDto body)
        {
            try
            {
                if (body?.Priorities == null)
                {
                    return ApiResult.Fail("Invalid payload: 'priorities' array is missing or null.");
                }

                if (body.Priorities.Count == 0)
                {
                    return ApiResult.Ok();
                }

                var warnings = new List<string>();
                foreach (var colonistPriorityUpdate in body.Priorities)
                {
                    var result = SetColonistWorkPriority(colonistPriorityUpdate);
                    if (result.Success == false)
                    {
                        warnings.AddRange(result.Errors);
                    }
                }

                if (warnings.Count > 0)
                {
                    string combinedWarnings = string.Join("; ", warnings);

                    if (warnings.Count == body.Priorities.Count)
                    {
                        return ApiResult.Fail(combinedWarnings);
                    }
                    return ApiResult.Partial(warnings);
                }

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult SetColonistWorkPriority(WorkPriorityRequestDto body)
        {
            try
            {
                // Find the pawn by thingIDNumber
                Pawn pawn = PawnHelper.FindPawnById(body.Id);
                if (pawn == null)
                {
                    return ApiResult.Fail($"Could not find pawn with ID {body.Id}");
                }

                // Find the WorkTypeDef by defName
                WorkTypeDef workTypeDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(body.Work);
                if (workTypeDef == null)
                {
                    return ApiResult.Fail($"Could not find WorkTypeDef with defName {body.Work}");
                }

                // Check if pawn has work settings initialized
                if (pawn.workSettings == null || !pawn.workSettings.EverWork)
                {
                    return ApiResult.Fail(
                        $"Pawn {pawn.LabelShort} does not have work settings initialized"
                    );
                }

                // Check if the work type is disabled for this pawn
                if (body.Priority != 0 && pawn.WorkTypeIsDisabled(workTypeDef))
                {
                    return ApiResult.Fail(
                        $"Cannot set priority for disabled work type {workTypeDef.defName} on pawn {pawn.LabelShort}"
                    );
                }

                // Validate priority range (0-9)
                if (body.Priority < 0 || body.Priority > 9)
                {
                    return ApiResult.Fail($"Invalid priority {body.Priority}. Must be between 0 and 4");
                }

                // Set the priority
                pawn.workSettings.SetPriority(workTypeDef, body.Priority);

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult SetTimeAssignment(PawnTimeAssignmentRequestDto body)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(body.PawnId);
                TimeAssignmentDef assignmentDef = DefDatabase<TimeAssignmentDef>
                    .AllDefs.Where(p => p.defName.ToLower() == body.Assignment.ToLower())
                    .FirstOrDefault();
                if (assignmentDef == null)
                {
                    return ApiResult.Fail(
                        $"Failed to find assignment def with {body.Assignment} name"
                    );
                }
                pawn.timetable.SetAssignment(body.Hour, assignmentDef);

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult<List<ApiV1PawnDetailedDto>> GetColonistsDetailedV1()
        {
            List<ApiV1PawnDetailedDto> result = new List<ApiV1PawnDetailedDto>();
            foreach (PawnDetailedRequestDto data in ColonistsHelper.GetColonistsDetailedDtos())
            {
                result.Add(new ApiV1PawnDetailedDto
                {
                    Colonist = data.Pawn,
                    BodySize = data.Detailes.BodySize,
                    Sleep = data.Detailes.Sleep,
                    Comfort = data.Detailes.Comfort,
                    Beauty = data.Detailes.Beauty,
                    Joy = data.Detailes.Joy,
                    Energy = data.Detailes.Energy,
                    DrugsDesire = data.Detailes.DrugsDesire,
                    SurroundingBeauty = data.Detailes.SurroundingBeauty,
                    FreshAir = data.Detailes.FreshAir,
                    ColonistWorkInfo = data.Detailes.WorkInfo,
                    MoodInfo = data.Detailes.MoodInfo,
                    PoliciesInfo = data.Detailes.PoliciesInfo,
                    ColonistMedicalInfo = data.Detailes.MedicalInfo,
                    SocialInfo = data.Detailes.SocialInfo,
                });
            }
            return ApiResult<List<ApiV1PawnDetailedDto>>.Ok(result);
        }

        public ApiResult<ApiV1PawnDetailedDto> GetColonistDetailedV1(int pawnId)
        {
            try
            {
                PawnDetailedRequestDto data = ColonistsHelper.GetSingleColonistDetailedDto(pawnId);

                ApiV1PawnDetailedDto result = new ApiV1PawnDetailedDto
                {
                    Colonist = data.Pawn,
                    BodySize = data.Detailes.BodySize,
                    Sleep = data.Detailes.Sleep,
                    Comfort = data.Detailes.Comfort,
                    Beauty = data.Detailes.Beauty,
                    Joy = data.Detailes.Joy,
                    Energy = data.Detailes.Energy,
                    DrugsDesire = data.Detailes.DrugsDesire,
                    SurroundingBeauty = data.Detailes.SurroundingBeauty,
                    FreshAir = data.Detailes.FreshAir,
                    ColonistWorkInfo = data.Detailes.WorkInfo,
                    MoodInfo = data.Detailes.MoodInfo,
                    PoliciesInfo = data.Detailes.PoliciesInfo,
                    ColonistMedicalInfo = data.Detailes.MedicalInfo,
                    SocialInfo = data.Detailes.SocialInfo,
                };

                if (result == null)
                {
                    return ApiResult<ApiV1PawnDetailedDto>.Fail($"Failed to find pawn with id: {pawnId}");
                }
                return ApiResult<ApiV1PawnDetailedDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<ApiV1PawnDetailedDto>
                    .Fail($"Failed to find pawn with id: {pawnId} - {ex.Message}");
            }
        }
    }
}
