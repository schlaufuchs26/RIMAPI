using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    /// <summary>
    /// The Animals tab's training card (fox ticket #4212, gap 5). A hauling dog
    /// needs training priorities, and the mod could tame and slaughter and not
    /// train, so the colony had a dog that would never haul.
    ///
    /// Everything here is the game's own card: TrainingCardUtility's checkbox
    /// (Pawn_TrainingTracker.SetWantedRecursive, which also sets the prerequisites
    /// the trick needs), its master button (TrainableUtility.CanBeMaster) and the
    /// area dropdown (playerSettings.AreaRestrictionInPawnCurrentMap).
    /// </summary>
    public class AnimalTrainingService : IAnimalTrainingService
    {
        public ApiResult<AnimalTrainingDto> Get(int pawnId)
        {
            try
            {
                Pawn animal = FindAnimal(pawnId, out string problem);
                if (animal == null)
                    return ApiResult<AnimalTrainingDto>.Fail(problem);

                return ApiResult<AnimalTrainingDto>.Ok(ReadBack(animal));
            }
            catch (Exception ex)
            {
                return ApiResult<AnimalTrainingDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<AnimalTrainingDto> SetTraining(SetTrainingRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Trainable))
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.UnknownTrainable(request?.Trainable ?? "")
                    );

                Pawn animal = FindAnimal(request.PawnId, out string problem);
                if (animal == null)
                    return ApiResult<AnimalTrainingDto>.Fail(problem);

                TrainableDef trainable = DefDatabase<TrainableDef>.GetNamedSilentFail(
                    request.Trainable.Trim()
                );
                if (trainable == null)
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.UnknownTrainable(request.Trainable)
                    );

                // The card's checkbox: CanAssignToTrain says whether the trick is
                // shown at all and whether it can be switched on right now.
                AcceptanceReport canTrain = animal.training.CanAssignToTrain(
                    trainable,
                    out bool visible
                );
                if (!visible)
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.NotShown(animal.LabelShortCap, trainable.LabelCap)
                    );
                if (request.Wanted && !canTrain.Accepted)
                    return ApiResult<AnimalTrainingDto>.Fail(canTrain.Reason);

                if (animal.training.GetWanted(trainable) == request.Wanted)
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.AlreadyWanted(trainable.LabelCap, request.Wanted)
                    );

                // The checkbox sets the prerequisites with the trick, as the card
                // does, so a caller need not know the tree.
                animal.training.SetWantedRecursive(trainable, request.Wanted);

                return ApiResult<AnimalTrainingDto>.Ok(ReadBack(animal));
            }
            catch (Exception ex)
            {
                return ApiResult<AnimalTrainingDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<AnimalTrainingDto> SetMaster(AnimalMasterRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<AnimalTrainingDto>.Fail(AnimalTrainingRules.UnknownMaster(0));

                Pawn animal = FindAnimal(request.PawnId, out string problem);
                if (animal == null)
                    return ApiResult<AnimalTrainingDto>.Fail(problem);

                Pawn master = request.MasterId == 0 ? null : PawnHelper.FindPawnById(request.MasterId);
                if (request.MasterId != 0 && master == null)
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.UnknownMaster(request.MasterId)
                    );

                // The master button only appears once the animal has learned
                // obedience; before that the game refuses a master of its own.
                if (master != null && !animal.training.HasLearned(TrainableDefOf.Obedience))
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.NoMasterNeeded(animal.LabelShortCap)
                    );

                if (master != null)
                {
                    AcceptanceReport canBeMaster = TrainableUtility.CanBeMaster(master, animal);
                    if (!canBeMaster.Accepted)
                        return ApiResult<AnimalTrainingDto>.Fail(canBeMaster.Reason);
                }

                if (animal.playerSettings.Master == master)
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.AlreadyMaster(
                            animal.LabelShortCap,
                            master?.LabelShortCap ?? "no master"
                        )
                    );

                animal.playerSettings.Master = master;
                return ApiResult<AnimalTrainingDto>.Ok(ReadBack(animal));
            }
            catch (Exception ex)
            {
                return ApiResult<AnimalTrainingDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<AnimalTrainingDto> SetArea(AnimalAreaRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<AnimalTrainingDto>.Fail(AnimalTrainingRules.NoMap("the animal"));

                Pawn animal = FindAnimal(request.PawnId, out string problem);
                if (animal == null)
                    return ApiResult<AnimalTrainingDto>.Fail(problem);
                if (animal.MapHeld == null)
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.NoMap(animal.LabelShortCap)
                    );

                List<Area> areas = animal.MapHeld.areaManager.AllAreas;
                Area area = null;
                if (!string.IsNullOrWhiteSpace(request.Area))
                {
                    List<Area> matches = areas
                        .Where(a =>
                            string.Equals(a.Label, request.Area.Trim(), StringComparison.OrdinalIgnoreCase)
                        )
                        .ToList();
                    if (matches.Count == 0)
                        return ApiResult<AnimalTrainingDto>.Fail(
                            AnimalTrainingRules.UnknownArea(
                                request.Area,
                                areas.Select(a => a.Label)
                            )
                        );
                    area = matches[0];
                }

                if (animal.playerSettings.AreaRestrictionInPawnCurrentMap == area)
                    return ApiResult<AnimalTrainingDto>.Fail(
                        AnimalTrainingRules.AlreadyArea(
                            animal.LabelShortCap,
                            area?.Label ?? "anywhere"
                        )
                    );

                animal.playerSettings.AreaRestrictionInPawnCurrentMap = area;
                return ApiResult<AnimalTrainingDto>.Ok(ReadBack(animal));
            }
            catch (Exception ex)
            {
                return ApiResult<AnimalTrainingDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<AnimalTrainingDto> SetFollow(AnimalFollowRequest request)
        {
            try
            {
                if (request == null || (!request.FollowDrafted.HasValue && !request.FollowFieldwork.HasValue))
                    return ApiResult<AnimalTrainingDto>.Fail(AnimalTrainingRules.NothingToChange());

                Pawn animal = FindAnimal(request.PawnId, out string problem);
                if (animal == null)
                    return ApiResult<AnimalTrainingDto>.Fail(problem);

                // The card's two checkboxes beside the master button.
                if (request.FollowDrafted.HasValue)
                    animal.playerSettings.followDrafted = request.FollowDrafted.Value;
                if (request.FollowFieldwork.HasValue)
                    animal.playerSettings.followFieldwork = request.FollowFieldwork.Value;

                return ApiResult<AnimalTrainingDto>.Ok(ReadBack(animal));
            }
            catch (Exception ex)
            {
                return ApiResult<AnimalTrainingDto>.Fail(ex.ToString());
            }
        }

        private static Pawn FindAnimal(int pawnId, out string problem)
        {
            problem = null;
            Pawn pawn = PawnHelper.FindPawnById(pawnId);
            if (pawn == null)
            {
                problem = AnimalTrainingRules.UnknownPawn(pawnId);
                return null;
            }
            if (pawn.training == null || pawn.playerSettings == null)
            {
                problem = AnimalTrainingRules.NotAnAnimal(pawn.LabelShortCap);
                return null;
            }
            return pawn;
        }

        private static AnimalTrainingDto ReadBack(Pawn animal)
        {
            var trainables = new List<TrainableDto>();
            foreach (TrainableDef td in DefDatabase<TrainableDef>.AllDefsListForReading)
            {
                AcceptanceReport canTrain = animal.training.CanAssignToTrain(td, out bool visible);
                trainables.Add(
                    new TrainableDto
                    {
                        Trainable = td.defName,
                        Label = td.LabelCap,
                        Visible = visible,
                        CanAssign = canTrain.Accepted,
                        Refusal = canTrain.Accepted ? null : canTrain.Reason,
                        Learned = animal.training.HasLearned(td),
                        Wanted = animal.training.GetWanted(td),
                    }
                );
            }

            return new AnimalTrainingDto
            {
                PawnId = animal.thingIDNumber,
                Name = animal.LabelShortCap,
                Race = animal.def.defName,
                Master = animal.playerSettings.Master?.LabelShortCap,
                MasterId = animal.playerSettings.Master?.thingIDNumber,
                Area = animal.playerSettings.AreaRestrictionInPawnCurrentMap?.Label,
                FollowDrafted = animal.playerSettings.followDrafted,
                FollowFieldwork = animal.playerSettings.followFieldwork,
                Trainables = trainables,
            };
        }
    }
}
