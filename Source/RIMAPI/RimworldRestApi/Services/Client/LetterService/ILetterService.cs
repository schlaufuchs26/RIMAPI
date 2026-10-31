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
        ApiResult<LetterDismissResultDto> DismissLetter(LetterDismissRequestDto request);
        ApiResult<LetterAnswerResultDto> AnswerLetter(LetterAnswerRequestDto request);
    }
}
