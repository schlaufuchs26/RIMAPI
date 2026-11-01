using System;
using System.Collections.Generic;
using System.Reflection;
using RIMAPI.Core;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    // The game's kill tales (fox ticket #3643). A pawn who dies at another's
    // hand leaves a tale (TaleRecorder.RecordTale with KilledLongRange,
    // KilledMelee, KilledMortar, KilledMajorThreat or KilledCapacity), and the
    // tale names the killer, the victim and the weapon's def. The wait read it
    // to tell a kill from a raid that walked off the map, which the fight block
    // could not: both just disappeared from it.
    //
    // The members are looked up by name the way ArchiveText does it, because
    // the tale classes keep theirs private or protected in Verse. A member this
    // game build does not have yields null instead of an exception, so an old
    // build answers fewer fields, not a 500.
    public class DeathService : IDeathService
    {
        // A run asks for the newest few; the cap keeps a wide `since` from
        // answering a whole colony history over the bridge.
        private const int MaxDeaths = 100;
        private const int DefaultDeaths = 25;

        // The prefix every kill tale's defName carries (KilledLongRange,
        // KilledMelee, ...). A tale outside it is not a death.
        private const string KillTalePrefix = "Killed";

        public ApiResult<List<DeathDto>> ListDeaths(int since, int limit)
        {
            try
            {
                if (limit <= 0)
                    limit = DefaultDeaths;
                if (limit > MaxDeaths)
                    limit = MaxDeaths;

                var manager = Find.TaleManager;
                if (manager == null)
                    return ApiResult<List<DeathDto>>.Fail("no tale manager: no colony is loaded");
                var tales = manager.AllTalesListForReading;
                if (tales == null)
                    return ApiResult<List<DeathDto>>.Ok(new List<DeathDto>());

                int offset = TickOffset();
                var entries = new List<DeathDto>();
                foreach (var tale in tales)
                {
                    var dto = ToDto(tale, offset);
                    if (dto == null)
                        continue;
                    if (since > 0 && dto.Tick < since)
                        continue;
                    entries.Add(dto);
                }

                entries.Sort((a, b) => a.Tick.CompareTo(b.Tick));
                if (entries.Count > limit)
                    entries = entries.GetRange(entries.Count - limit, limit);
                return ApiResult<List<DeathDto>>.Ok(entries);
            }
            catch (Exception ex)
            {
                return ApiResult<List<DeathDto>>.Fail(ex.Message);
            }
        }

        // TickOffset turns a tale's date into a game tick: a tale is dated in
        // TicksAbs (the world clock, which carries the founding tick), and every
        // other read answers TicksGame.
        private static int TickOffset()
        {
            var ticks = Find.TickManager;
            if (ticks == null)
                return 0;
            return ticks.TicksAbs - ticks.TicksGame;
        }

        // ToDto turns one tale into a death, or null when the tale is not a
        // kill: a birth, a wound or a mental break is no death.
        private static DeathDto ToDto(Tale tale, int offset)
        {
            if (tale == null)
                return null;
            var def = tale.def;
            string defName = def != null ? def.defName : "";
            if (string.IsNullOrEmpty(defName) || !defName.StartsWith(KillTalePrefix, StringComparison.Ordinal))
                return null;

            // Tale_DoublePawn keeps the killer in firstPawnData and the victim
            // in secondPawnData; the kill tales declare those symbols
            // themselves (firstPawnSymbol KILLER, secondPawnSymbol VICTIM).
            var killerData = Read(tale, "firstPawnData");
            var victimData = Read(tale, "secondPawnData");
            var weaponDef = Read(Read(tale, "defData"), "def") as Def;

            var dto = new DeathDto
            {
                Tick = tale.date > 0 ? tale.date - offset : 0,
                Def = defName,
                DefLabel = def.label ?? "",
                Weapon = weaponDef != null ? weaponDef.label ?? "" : "",
            };
            FillPawn(dto, killerData, victimData);
            return dto;
        }

        // FillPawn names the killer and the victim. The pawn is a plain field on
        // the tale's data, so it survives a load with the object; a pawn the
        // game has already discarded leaves only the tale's kind and name, and
        // the id then stays 0 so a caller matches on the name.
        private static void FillPawn(DeathDto dto, object killerData, object victimData)
        {
            var killer = PawnOf(killerData);
            if (killer != null)
            {
                dto.KillerId = killer.thingIDNumber;
                dto.Killer = Label(killer);
            }
            var victim = PawnOf(victimData);
            if (victim != null)
            {
                dto.VictimId = victim.thingIDNumber;
                dto.Victim = Label(victim);
            }
            if (string.IsNullOrEmpty(dto.Victim))
                dto.Victim = NameOf(victimData);
            dto.VictimKind = KindOf(victim, victimData);
            dto.VictimFaction = FactionOf(victim, victimData);
        }

        // PawnOf reads the pawn out of a TaleData_Pawn. The field is the one the
        // tale writes; the property is the resolved form some builds carry.
        private static Pawn PawnOf(object data)
        {
            if (data == null)
                return null;
            return (Read(data, "pawn") ?? Read(data, "Pawn")) as Pawn;
        }

        // NameOf is the tale's own record of the pawn's name, which stays when
        // the pawn object is gone.
        private static string NameOf(object data)
        {
            var name = Read(data, "name");
            return name != null ? name.ToString() : "";
        }

        // KindOf names the pawn's kind (a defName such as Raider), from the pawn
        // or from the tale's own kind def.
        private static string KindOf(Pawn pawn, object data)
        {
            if (pawn != null && pawn.kindDef != null)
                return pawn.kindDef.defName;
            var kind = Read(data, "kind") as PawnKindDef;
            return kind != null ? kind.defName : "";
        }

        private static string FactionOf(Pawn pawn, object data)
        {
            if (pawn != null && pawn.Faction != null)
                return pawn.Faction.Name;
            var faction = Read(data, "faction") as Faction;
            return faction != null ? faction.Name : "";
        }

        private static string Label(Pawn pawn)
        {
            try
            {
                return pawn.LabelShort;
            }
            catch (Exception)
            {
                return "";
            }
        }

        // Read looks a property or field up by name up the type hierarchy, the
        // way ArchiveText and the window read do: the members these reads need
        // are private or protected in Verse.
        private static object Read(object obj, string name)
        {
            if (obj == null)
                return null;
            for (var type = obj.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(obj, null);
                var field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(obj);
            }
            return null;
        }
    }
}
