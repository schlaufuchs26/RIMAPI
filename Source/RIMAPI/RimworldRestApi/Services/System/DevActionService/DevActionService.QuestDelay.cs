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
    /// The quest_delay op (fox ticket #4001): move the pending delay of the
    /// newest quest to N ticks from now. A hospitality quest builds its shuttle
    /// only after 5 to 25 in-game days, and a `ticks` call runs 20,000 ticks, so
    /// reaching that state took about 40 calls, and a two-star raid kidnapped
    /// and killed the lodger on the way.
    ///
    /// A delay is not a countdown of its own: QuestPart_Delay.TicksLeft is
    /// enableTick + delayTicks - TicksGame. The op therefore writes delayTicks
    /// for the span the caller asked for and leaves the part's own start tick
    /// alone. The part keeps its type, so a shuttle delay still lands a shuttle
    /// and a leave delay still sends one away when it ends; the op only moves
    /// the clock.
    /// </summary>
    public partial class DevActionService
    {
        private DevActionResultDto SetQuestDelay(DevActionRequest request)
        {
            int want = request.Ticks.Value;
            int now = Find.TickManager.TicksGame;
            Quest quest = null;
            QuestPart_Delay part = null;
            int before = 0;
            int others = 0;

            foreach (Quest candidate in Find.QuestManager.QuestsListForReading)
            {
                if (!string.IsNullOrEmpty(request.Name) && !MatchesQuest(candidate, request.Name))
                {
                    continue;
                }
                List<QuestPart_Delay> pending = PendingDelays(candidate);
                if (pending.Count == 0) continue;
                // The soonest pending delay is the one that holds the quest's
                // next state, so that is the one a check waits for.
                QuestPart_Delay soonest = pending[0];
                foreach (QuestPart_Delay delay in pending)
                {
                    if (delay.TicksLeft < soonest.TicksLeft) soonest = delay;
                }
                if (quest == null || candidate.id > quest.id)
                {
                    quest = candidate;
                    part = soonest;
                    before = soonest.TicksLeft;
                    others = pending.Count - 1;
                }
            }

            if (quest == null)
            {
                throw new DevRefusal(NoPendingDelay(request.Name));
            }

            // TicksLeft = EnableTick + delayTicks - TicksGame, so this is the
            // delay that leaves exactly `want` ticks on the part.
            int delayTicks = want + (now - part.EnableTick);
            part.delayTicks = delayTicks < 0 ? 0 : delayTicks;

            bool ended = false;
            int after = part.TicksLeft;
            if (after <= 0)
            {
                // The game's own "End <part>" button in the debug window: the
                // delay completes here instead of on a tick the caller would
                // have to run separately.
                part.DebugForceEnd();
                ended = true;
                after = 0;
            }

            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                QuestDelay = new DevQuestDelayStateDto
                {
                    QuestId = quest.id,
                    QuestName = quest.name,
                    ScriptDefName = quest.root == null ? "" : quest.root.defName,
                    PartType = part.GetType().Name,
                    Before = before,
                    After = after,
                    Ended = ended,
                    Others = others,
                },
            };
            result.Detail =
                "quest "
                + quest.id
                + " \""
                + quest.name
                + "\" ("
                + result.QuestDelay.ScriptDefName
                + "): its "
                + result.QuestDelay.PartType
                + " held "
                + before.ToStringTicksToPeriod()
                + " ("
                + before
                + " ticks), "
                + (ended
                    ? "and the delay ended in this call, so the quest's next part runs now"
                    : "and now holds "
                        + after
                        + " ticks ("
                        + after.ToStringTicksToPeriod()
                        + "), which the game fires on its next tick")
                + (others > 0
                    ? "; "
                        + others
                        + (others == 1 ? " other delay stands" : " other delays stand")
                        + " in that quest as they were"
                    : "");
            return result;
        }

        /// <summary>The enabled delays of one quest, in the order the quest holds them.</summary>
        private static List<QuestPart_Delay> PendingDelays(Quest quest)
        {
            return quest
                .PartsListForReading.OfType<QuestPart_Delay>()
                .Where(d => d.State == QuestPartState.Enabled)
                .ToList();
        }

        /// <summary>
        /// True when a quest answers the request's `name`: a piece of its own
        /// name or of its script's defName, either case.
        /// </summary>
        private static bool MatchesQuest(Quest quest, string name)
        {
            if (quest.name != null && quest.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return quest.root != null
                && quest.root.defName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// The refusal for a colony with no pending delay to move: it names the
        /// quests that hold one, or says why none runs yet.
        /// </summary>
        private static string NoPendingDelay(string name)
        {
            List<string> standing = new List<string>();
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (PendingDelays(quest).Count == 0) continue;
                standing.Add(
                    quest.id
                        + " \""
                        + quest.name
                        + "\" ("
                        + (quest.root == null ? "" : quest.root.defName)
                        + ")"
                );
            }
            if (standing.Count == 0)
            {
                return "no quest in this colony holds a pending delay right now: a quest's delays only "
                    + "run once it is accepted (quest_accept), and the `quests` read lists what stands";
            }
            return "no quest with a pending delay matches "
                + (string.IsNullOrEmpty(name) ? "the call" : "\"" + name + "\"")
                + "; the quests that hold one are "
                + string.Join(", ", standing);
        }
    }
}
