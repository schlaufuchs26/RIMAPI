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
    // A quest offer can be read but not taken through the bridge (ticket
    // #3216): the letter that announces it only offers "View quest", "Jump to
    // location" and "Close", and the accept button lives in the game's Quests
    // tab. This service runs the same path that button runs
    // (MainTabWindow_Quests.DoAcceptButton and AcceptQuestByInterface): the
    // game's own CanAcceptQuest report decides whether the quest can be taken,
    // a reward choice is chosen through QuestPart_Choice.Choose, and
    // Quest.Accept does the rest, so the quest parts, letters and thoughts are
    // the game's own.
    public class QuestService : IQuestService
    {
        public ApiResult<QuestAcceptResultDto> AcceptQuest(QuestAcceptRequestDto request)
        {
            try
            {
                if (request == null)
                    return ApiResult<QuestAcceptResultDto>.Fail("the request needs the quest's id");
                if (Find.QuestManager == null)
                    return ApiResult<QuestAcceptResultDto>.Fail("no quest manager: no colony is loaded");

                var quest = Find.QuestManager.QuestsListForReading
                    .FirstOrDefault(q => q != null && q.id == request.Id);
                if (quest == null)
                    return ApiResult<QuestAcceptResultDto>.Fail(
                        $"no quest with id {request.Id}; the quests read prints the ids");

                var result = new QuestAcceptResultDto { Id = quest.id, Name = quest.name };

                // Only an offered quest can be accepted; the game's own state
                // check, so a second call on the same id says what happened
                // instead of running the accept twice.
                if (quest.State != QuestState.NotYetAccepted)
                {
                    result.State = quest.State.ToString();
                    result.Reason = quest.State == QuestState.EndedOfferExpired
                        ? $"quest {quest.id} {Quoting.Quote(quest.name)} expired before it was accepted"
                        : $"quest {quest.id} {Quoting.Quote(quest.name)} is {quest.State}, not on offer; only a NotYetAccepted quest can be accepted";
                    return ApiResult<QuestAcceptResultDto>.Ok(result);
                }

                // The game's own report, the same one its accept button greys
                // out with: unmet requirements (a faction, a colonist, a
                // condition) are named here instead of failing silently.
                AcceptanceReport report = QuestUtility.CanAcceptQuest(quest);
                result.CanAccept = report.Accepted;
                result.AcceptReason = report.Reason ?? "";
                if (!report.Accepted)
                {
                    result.Reason = "the game refuses this quest: " +
                        (string.IsNullOrEmpty(report.Reason) ? "no reason given" : report.Reason);
                    result.State = quest.State.ToString();
                    return ApiResult<QuestAcceptResultDto>.Ok(result);
                }

                // A quest with reward choices draws one accept button per
                // choice in the Quests tab, and the choice decides which quest
                // parts run; accepting without it would take a quest whose
                // rewards were never chosen.
                var choicePart = quest.PartsListForReading.OfType<QuestPart_Choice>().FirstOrDefault();
                QuestPart_Choice.Choice chosen = null;
                if (choicePart != null && choicePart.choices.Count > 0)
                {
                    result.Choices = GameEventsHelper.GetQuestChoices(quest);
                    if (!request.Choice.HasValue)
                    {
                        result.Reason =
                            $"quest {quest.id} offers {choicePart.choices.Count} reward choice(s); accept again with choice=<index>";
                        result.State = quest.State.ToString();
                        return ApiResult<QuestAcceptResultDto>.Ok(result);
                    }
                    int index = request.Choice.Value;
                    if (index < 0 || index >= choicePart.choices.Count)
                    {
                        result.Reason =
                            $"choice {index} is out of range: the quest offers 0 to {choicePart.choices.Count - 1}";
                        result.State = quest.State.ToString();
                        return ApiResult<QuestAcceptResultDto>.Ok(result);
                    }
                    chosen = choicePart.choices[index];
                    result.Choice = index;
                }

                // Whether the quest needs a named colonist, read the way the
                // game reads it for a choice: the parts the taken choice keeps.
                bool requiresAccepter = RequiresAccepter(quest, choicePart, chosen);

                Pawn accepter = null;
                if (requiresAccepter || request.PawnId.HasValue)
                {
                    if (request.PawnId.HasValue)
                    {
                        accepter = FindPawn(request.PawnId.Value);
                        if (accepter == null)
                        {
                            result.Reason =
                                $"no colonist with id {request.PawnId.Value} is on the map; the colonists read prints the ids";
                            result.State = quest.State.ToString();
                            return ApiResult<QuestAcceptResultDto>.Ok(result);
                        }
                        if (!QuestUtility.CanPawnAcceptQuest(accepter, quest))
                        {
                            result.Reason =
                                $"{accepter.LabelShortCap} cannot accept this quest: the game's own check refuses the pawn (free colonist, not downed, not suspended, and every requirement of the quest)";
                            result.State = quest.State.ToString();
                            return ApiResult<QuestAcceptResultDto>.Ok(result);
                        }
                    }
                    else
                    {
                        accepter = Candidates(quest).FirstOrDefault();
                        if (accepter == null)
                        {
                            result.Reason =
                                "no colonist can accept this quest: the game's own check refuses every free colonist, so name none and wait, or check the quest's requirements";
                            result.State = quest.State.ToString();
                            return ApiResult<QuestAcceptResultDto>.Ok(result);
                        }
                    }
                }

                // The game's own order: the choice first, then the accept, as
                // AcceptQuestByInterface's preAcceptAction does it.
                if (chosen != null)
                    choicePart.Choose(chosen);
                quest.Accept(accepter);

                result.Accepted = quest.State == QuestState.Ongoing;
                result.State = quest.State.ToString();
                if (accepter != null)
                {
                    result.Accepter = accepter.LabelShortCap;
                    result.AccepterPawnId = accepter.thingIDNumber;
                }
                if (!result.Accepted && string.IsNullOrEmpty(result.Reason))
                    result.Reason = "the game did not move the quest out of the offer state";
                LogApi.Info($"[QuestService] Accepted quest {quest.id} {quest.name}" +
                    (accepter != null ? $" with {accepter.LabelShortCap}" : " for the colony"));
                return ApiResult<QuestAcceptResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<QuestAcceptResultDto>.Fail(ex.Message);
            }
        }

        // RequiresAccepter mirrors MainTabWindow_Quests.DoRewards: with a reward
        // choice taken, the parts of the other choices are dropped before the
        // remaining parts are asked whether they need an accepter.
        private static bool RequiresAccepter(Quest quest, QuestPart_Choice choicePart,
            QuestPart_Choice.Choice chosen)
        {
            if (chosen == null)
                return quest.RequiresAccepter;
            foreach (var part in quest.PartsListForReading)
            {
                if (part == null)
                    continue;
                if (DroppedByChoice(choicePart, chosen, part))
                    continue;
                if (part.RequiresAccepter)
                    return true;
            }
            return false;
        }

        private static bool DroppedByChoice(QuestPart_Choice choicePart, QuestPart_Choice.Choice chosen,
            QuestPart part)
        {
            foreach (var other in choicePart.choices)
            {
                if (other == chosen || other == null)
                    continue;
                if (other.questParts.Contains(part) && !chosen.questParts.Contains(part))
                    return true;
            }
            return false;
        }

        // Candidates is the game's own list for the accepter: living free
        // colonists on maps, caravans and transporters, without suspended ones.
        private static IEnumerable<Pawn> Candidates(Quest quest)
        {
            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended
                .Where(p => p != null && QuestUtility.CanPawnAcceptQuest(p, quest));
        }

        private static Pawn FindPawn(int pawnId)
        {
            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended
                .FirstOrDefault(p => p != null && p.thingIDNumber == pawnId)
                ?? Find.Maps.SelectMany(m => m.mapPawns.AllPawns)
                    .FirstOrDefault(p => p != null && p.thingIDNumber == pawnId);
        }
    }
}
