using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface ILetterService
    {
        ApiResult<List<LetterDto>> ListLetters();
        ApiResult<LetterDismissResultDto> DismissLetter(LetterDismissRequestDto request);
    }
}
