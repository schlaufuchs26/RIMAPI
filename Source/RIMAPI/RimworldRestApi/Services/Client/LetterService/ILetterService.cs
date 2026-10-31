using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface ILetterService
    {
        // markSeen false is a read-only look: it answers which letters are new
        // but leaves the stack unseen for the read that reports them.
        ApiResult<List<LetterDto>> ListLetters(bool markSeen);
        // ListArchivedLetters answers the letters the game received at or after
        // since, standing or not: the stack drops a letter whose target is gone,
        // so a letter that paused the game can be off it by the next look
        // (ticket #3357).
        ApiResult<List<LetterDto>> ListArchivedLetters(int since, int limit);
        ApiResult<LetterDismissResultDto> DismissLetter(LetterDismissRequestDto request);
        ApiResult<LetterAnswerResultDto> AnswerLetter(LetterAnswerRequestDto request);
    }
}
