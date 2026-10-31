using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RIMAPI.Services
{
    // The game's message archive (ticket #3336): the little notifications a
    // player sees for a moment and then only finds in the archive, e.g.
    // "Rice plant has died because of cold." or "Maskinnen is being attacked by
    // Haplo!". They are neither alerts nor letters, so no other read showed
    // them, and the only way to one was a save file parsed on disk.
    //
    // The archive is the same list the save writes under <history><archive>
    // (Find.Archive, measured 2026-10-31), so this read answers exactly what
    // the save-backed read answered, without a save and up to the current
    // tick instead of up to the last save.
    //
    // Letters share that archive, and a death letter is where a death's cause
    // is written, so this read answers them too (ticket #3400): Class says
    // which kind an entry is, Label a letter's title. A caller that wants only
    // the notifications filters on Class = "Message"; that is what the wait
    // report and the digest do, because the letters read already reports the
    // stack.
    public class MessageService : IMessageService
    {
        // A run asks for the newest few; the cap keeps a wide `since` from
        // answering the whole colony history over the bridge.
        private const int MaxMessages = 200;
        private const int DefaultMessages = 25;

        public ApiResult<List<MessageDto>> ListMessages(int since, int limit)
        {
            try
            {
                if (limit <= 0)
                    limit = DefaultMessages;
                if (limit > MaxMessages)
                    limit = MaxMessages;

                var archive = Find.Archive;
                if (archive == null)
                    return ApiResult<List<MessageDto>>.Fail("no archive: no colony is loaded");

                var entries = new List<MessageDto>();
                foreach (var archivable in archive.ArchivablesListForReading)
                {
                    var dto = ToDto(archivable);
                    if (dto == null)
                        continue;
                    if (since > 0 && dto.Tick < since)
                        continue;
                    entries.Add(dto);
                }

                // Oldest first, so a caller reads them in the order they
                // happened; the newest `limit` survive.
                entries.Sort((a, b) => a.Tick.CompareTo(b.Tick));
                if (entries.Count > limit)
                    entries = entries.Skip(entries.Count - limit).ToList();
                return ApiResult<List<MessageDto>>.Ok(entries);
            }
            catch (Exception ex)
            {
                return ApiResult<List<MessageDto>>.Fail(ex.Message);
            }
        }

        // MessageTick is the tick the message was created. startingTick is the
        // field the save writes; a message without one reads 0, which a
        // caller treats as unknown rather than as the colony's first tick.
        private static int MessageTick(Message message)
        {
            return message.startingTick > 0 ? message.startingTick : 0;
        }

        // ToDto turns one archive entry into its DTO. Class is the name the save
        // writes as the entry's Class attribute, so a caller sees the same
        // kinds it saw from a save file. An archive entry that is neither a
        // message nor a letter answers null and is left out.
        private static MessageDto ToDto(IArchivable archivable)
        {
            var message = archivable as Message;
            if (message != null)
            {
                var dto = new MessageDto
                {
                    Class = "Message",
                    Def = message.def?.defName,
                    Text = message.text,
                    Tick = MessageTick(message),
                };
                FillTarget(dto, message.lookTargets);
                return dto;
            }

            var letter = archivable as Letter;
            if (letter == null)
                return null;
            var letterDto = new MessageDto
            {
                Class = letter.GetType().Name,
                Def = letter.def?.defName,
                Label = ArchiveText.FirstLine(letter.Label),
                Text = ArchiveText.FirstLine(ArchiveText.LetterBody(letter)),
                Tick = letter.arrivalTick,
            };
            FillTarget(letterDto, letter.lookTargets);
            return letterDto;
        }

        // FillTarget names what the entry is about: the thing it points at, or
        // the cell when the game gave no thing.
        private static void FillTarget(MessageDto dto, LookTargets lookTargets)
        {
            if (lookTargets == null)
                return;

            GlobalTargetInfo target = lookTargets.PrimaryTarget;
            if (target.Thing != null)
            {
                dto.HasTarget = true;
                dto.TargetLabel = target.Thing.LabelCap;
                dto.TargetDef = target.Thing.def?.defName;
                dto.X = target.Thing.Position.x;
                dto.Y = target.Thing.Position.y;
                dto.Z = target.Thing.Position.z;
                if (target.Thing.Map != null)
                    dto.MapId = target.Thing.Map.uniqueID;
            }
            else if (target.Cell.IsValid)
            {
                dto.HasTarget = true;
                dto.X = target.Cell.x;
                dto.Y = target.Cell.y;
                dto.Z = target.Cell.z;
                if (target.Map != null)
                    dto.MapId = target.Map.uniqueID;
            }
        }
    }
}
