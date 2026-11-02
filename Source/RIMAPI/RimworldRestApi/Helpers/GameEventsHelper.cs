using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RIMAPI.Core;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    class GameEventsHelper
    {
        public static QuestsDto GetQuestsDto(Map map)
        {
            var dto = new QuestsDto();

            List<Quest> allQuests = Find.QuestManager.QuestsListForReading;

            dto.HistoricalQuests.AddRange(
                allQuests.Where(quest => quest.Historical).Select(ToQuestDto)
            );

            dto.ActiveQuests.AddRange(
                allQuests.Where(quest => !quest.Historical).Select(ToQuestDto)
            );
            return dto;
        }

        // ToQuestDto is one quest as the quests read prints it, both lists
        // built from it. The accept fields carry the game's own report
        // (QuestUtility.CanAcceptQuest) and the reward choices, so a caller can
        // tell an offer it can take from one the game refuses before it calls
        // quest/accept (ticket #3216).
        private static QuestDto ToQuestDto(Quest quest)
        {
            try
            {
                return BuildQuestDto(quest);
            }
            catch (Exception ex)
            {
                // One unreadable quest must not fail the whole read: the offer
                // standing beside it is what a run needs to judge, and the game
                // keeps quests whose parts throw when they are read. The log
                // line carries the stack trace the answer cannot (fox ticket
                // #4164).
                LogApi.Error($"[GameEvents] quest {quest?.id} {quest?.name} could not be read: {ex}");
                return BrokenQuestDto(quest, ex);
            }
        }

        // BrokenQuestDto keeps what a quest says about itself without touching
        // the parts that threw, so the read names the quest and its failure
        // instead of dropping it silently.
        private static QuestDto BrokenQuestDto(Quest quest, Exception ex)
        {
            string state = "";
            try
            {
                state = quest?.State.ToString() ?? "";
            }
            catch
            {
                // the state itself is what threw; an empty one says so
            }
            return new QuestDto
            {
                Id = quest?.id ?? -1,
                Name = quest?.name ?? "",
                QuestDef = quest?.root?.defName ?? "Unknown",
                State = state,
                ReadError = ex.Message,
            };
        }

        private static QuestDto BuildQuestDto(Quest quest)
        {
            var dto = new QuestDto
            {
                Id = quest.id,
                QuestDef = quest.root?.defName ?? "Unknown",
                Name = quest.name,
                Description = quest.description.ToString(),
                State = quest.State.ToString(),
                ExpiryHours = GameTypesHelper.TicksToDays(quest.TicksUntilExpiry) * 24,
                Reward = GetQuestRewardString(quest),
                RequiresAccepter = quest.RequiresAccepter,
                Accepter = quest.AccepterPawn != null ? quest.AccepterPawn.LabelShortCap : "",
                // When the quest ended, which its id does not say: the id rises
                // with creation, so an old quest finished yesterday would sort
                // below one created late and finished days ago (fox #4080).
                EndedTick = quest.cleanupTick,
                EndedTicksAgo = quest.TicksSinceCleanup,
            };
            AcceptanceReport report = QuestUtility.CanAcceptQuest(quest);
            dto.CanAccept = report.Accepted;
            dto.AcceptReason = report.Reason ?? "";
            dto.Choices = GetQuestChoices(quest);
            return dto;
        }

        // GetQuestChoices names each reward choice the Quests tab draws an
        // accept button for, with its value and reward kinds.
        public static List<QuestChoiceDto> GetQuestChoices(Quest quest)
        {
            var choices = new List<QuestChoiceDto>();
            var choicePart = quest.PartsListForReading.OfType<QuestPart_Choice>().FirstOrDefault();
            if (choicePart == null)
                return choices;
            for (int i = 0; i < choicePart.choices.Count; i++)
            {
                var dto = new QuestChoiceDto { Index = i };
                var choice = choicePart.choices[i];
                if (choice != null)
                {
                    foreach (var reward in choice.rewards)
                    {
                        if (reward == null)
                            continue;
                        dto.TotalMarketValue += reward.TotalMarketValue;
                        dto.Rewards.Add(reward.GetType().Name);
                        dto.Items.AddRange(GetRewardItems(reward));
                    }
                }
                choices.Add(dto);
            }
            return choices;
        }

        // Reward_Items keeps the things it generated in `items` only while the
        // reward is unused; once the game hands it out or cleans it up it
        // remembers them as `itemDefs`, a private list of labels, and drops the
        // things. Reading only `items` answered a used reward with its class
        // alone (fox ticket #4009).
        private static readonly FieldInfo ItemDefsField = typeof(Reward_Items).GetField(
            "itemDefs",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        // Reward.usedOrCleanedUp is protected, the flag the game itself decides
        // a reward's stack elements by.
        private static readonly FieldInfo UsedOrCleanedUpField = typeof(Reward).GetField(
            "usedOrCleanedUp",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        // GetRewardItems names the things a Reward_Items holds or gave. The
        // class generates them when the quest is drawn (Reward_Items.InitFromValue
        // fills `items` from ThingSetMakerDefOf.Reward_ItemsStandard), so a
        // quest on offer has them; a reward the game already used keeps only
        // its remembered labels, which this reads instead of guessing (fox
        // tickets #3950, #4009).
        public static List<QuestChoiceItemDto> GetRewardItems(Reward reward)
        {
            var items = new List<QuestChoiceItemDto>();
            if (!(reward is Reward_Items rewardItems))
                return items;
            foreach (var thing in rewardItems.ItemsListForReading)
            {
                if (thing == null || thing.Destroyed)
                    continue;
                var inner = thing.GetInnerIfMinified() ?? thing;
                var item = new QuestChoiceItemDto
                {
                    // The label without the quality: LabelNoCount already
                    // carries it ("eltex shirt (excellent)"), and the caller
                    // gets the quality on its own field (fox ticket #3950).
                    Label = GenLabel.ThingLabel(inner.def, inner.Stuff, 1),
                    Count = thing.stackCount,
                    MarketValue = inner.MarketValue * thing.stackCount,
                    Quality = inner.TryGetQuality(out var quality) ? quality.GetLabel() : "",
                };
                items.Add(item);
            }
            // The game's own rule (Reward_Items.StackElements): a used reward
            // reads from the labels it remembered, not from things that are
            // gone. The count fallback keeps a bridge honest whose game renamed
            // the flag.
            if (items.Count == 0 || IsUsedOrCleanedUp(rewardItems))
            {
                items.Clear();
                AddRememberedItems(rewardItems, items);
            }
            return items;
        }

        // IsUsedOrCleanedUp is the game's own verdict on a reward, read through
        // reflection because the field is protected.
        private static bool IsUsedOrCleanedUp(Reward reward)
        {
            if (UsedOrCleanedUpField == null)
                return false;
            return UsedOrCleanedUpField.GetValue(reward) is bool used && used;
        }

        // AddRememberedItems names what a used item reward gave, from the
        // labels Reward_Items.RememberItems wrote before the game dropped the
        // things; the Quests tab draws those same entries as icons
        // (QuestPartUtility.GetRewardStackElementsForThings). The pair carries
        // def, stuff and quality, so the label and the stack's market value
        // come out as they do for a reward still on offer (fox ticket #4009).
        private static void AddRememberedItems(
            Reward_Items rewardItems,
            List<QuestChoiceItemDto> items
        )
        {
            if (ItemDefsField == null)
                return;
            var remembered = ItemDefsField.GetValue(rewardItems) as IEnumerable;
            if (remembered == null)
                return;
            foreach (Reward_Items.RememberedItem entry in remembered)
            {
                var pair = entry.thing;
                if (pair.thing == null)
                    continue;
                items.Add(
                    new QuestChoiceItemDto
                    {
                        Label = GenLabel.ThingLabel(pair.thing, pair.stuff, 1),
                        Count = entry.stackCount,
                        MarketValue =
                            pair.GetStatValue(StatDefOf.MarketValue) * entry.stackCount,
                        // RememberItems records QualityCategory.Normal for a
                        // thing without CompQuality, so the def decides whether
                        // there is a quality to print.
                        Quality = pair.thing.HasComp(typeof(CompQuality))
                            ? pair.Quality.GetLabel()
                            : "",
                        Remembered = true,
                    }
                );
            }
        }

        public static List<string> GetQuestRewardString(Quest quest)
        {
            return quest
                .PartsListForReading.OfType<QuestPart_Choice>()
                .SelectMany(choicePart => choicePart.choices)
                // A null entry in choices is possible and GetQuestChoices
                // already skips it; this read must not throw on it either.
                .Where(choice => choice != null)
                .SelectMany(choice => choice.rewards)
                .Select(reward => reward?.ToString() ?? "Unknown")
                .ToList();
        }

        public static string GetIncidentImportance(IncidentDef def)
        {
            if (def.category == IncidentCategoryDefOf.ThreatBig)
                return "ThreatBig";
            if (def.category == IncidentCategoryDefOf.ThreatSmall)
                return "ThreatSmall";
            if (def.category == IncidentCategoryDefOf.DeepDrillInfestation)
                return "DeepDrillInfestation";
            if (def.category == IncidentCategoryDefOf.DiseaseHuman)
                return "DiseaseHuman";
            if (def.category == IncidentCategoryDefOf.GiveQuest)
                return "GiveQuest";
            if (def.category == IncidentCategoryDefOf.Misc)
                return "Misc";
            if (def.category == IncidentCategoryDefOf.Special)
                return "Special";
            return "None";
        }

        public static List<IncidentDto> GetIncidentsLog(Map map)
        {
            List<IncidentDto> incidentLog = new List<IncidentDto>();

            if (map?.storyState?.lastFireTicks == null)
                return incidentLog;

            foreach (KeyValuePair<IncidentDef, int> entry in map.storyState.lastFireTicks)
            {
                incidentLog.Add(
                    new IncidentDto
                    {
                        IncidentDef = entry.Key.defName,
                        Label = entry.Key.label,
                        Category = GetIncidentImportance(entry.Key),
                        IncidentHour = GameTypesHelper.TicksToDays(entry.Value) * 24,
                        DaysSinceOccurred = (
                            Find.TickManager.TicksGame - entry.Value
                        ).TicksToDays(),
                    }
                );
            }

            // Sort by most recent first
            incidentLog.Sort((a, b) => b.IncidentHour.CompareTo(a.IncidentHour));

            return incidentLog;
        }
    }
}
