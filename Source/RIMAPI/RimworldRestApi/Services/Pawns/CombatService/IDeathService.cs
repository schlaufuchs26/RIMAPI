using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    // Deaths the game recorded as kills (fox ticket #3643): the tale a kill
    // writes, with killer, victim and the weapon's def. A wait reads it to say
    // "died: shot by Lane (assault rifle)" where it used to say "gone".
    public interface IDeathService
    {
        // ListDeaths answers the kill tales at or after since (a game tick),
        // newest last. A death without an attacker writes no tale; the archive's
        // death letter answers that one.
        ApiResult<List<DeathDto>> ListDeaths(int since, int limit);
    }
}
