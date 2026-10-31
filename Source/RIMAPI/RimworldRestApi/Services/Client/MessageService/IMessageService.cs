using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IMessageService
    {
        // since 0 reads the whole archive; limit <= 0 means the default.
        ApiResult<List<MessageDto>> ListMessages(int since, int limit);
    }
}
