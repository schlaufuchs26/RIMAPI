using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    // The quests tab's accept button as a bridge call (ticket #3216). A quest
    // offer reaches the letter stack as a NewQuestLetter whose options are only
    // "View quest", "Jump to location" and "Close", so the offer could be read
    // but never taken; the quests read names the id, this service takes it.
    public interface IQuestService
    {
        ApiResult<QuestAcceptResultDto> AcceptQuest(QuestAcceptRequestDto request);
    }
}
