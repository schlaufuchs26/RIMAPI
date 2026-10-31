using System.Collections.Generic;
using System.Linq;
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
                    }
                }
                choices.Add(dto);
            }
            return choices;
        }

        public static List<string> GetQuestRewardString(Quest quest)
        {
            return quest
                .PartsListForReading.OfType<QuestPart_Choice>()
                .SelectMany(choicePart => choicePart.choices)
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
