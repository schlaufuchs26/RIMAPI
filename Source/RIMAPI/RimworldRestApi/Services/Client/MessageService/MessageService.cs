using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
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

                var messages = new List<Message>();
                foreach (var archivable in archive.ArchivablesListForReading)
                {
                    // Letters share the archive; they have their own read and
                    // are not what this one is about.
                    var message = archivable as Message;
                    if (message == null)
                        continue;
                    if (since > 0 && MessageTick(message) < since)
                        continue;
                    messages.Add(message);
                }

                // Oldest first, so a caller reads them in the order they
                // happened; the newest `limit` survive.
                messages.Sort((a, b) => MessageTick(a).CompareTo(MessageTick(b)));
                if (messages.Count > limit)
                    messages = messages.Skip(messages.Count - limit).ToList();

                var result = new List<MessageDto>(messages.Count);
                foreach (var message in messages)
                    result.Add(ToDto(message));
                return ApiResult<List<MessageDto>>.Ok(result);
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

        private static MessageDto ToDto(Message message)
        {
            var dto = new MessageDto
            {
                Def = message.def?.defName,
                Text = message.text,
                Tick = MessageTick(message),
            };
            var lookTargets = message.lookTargets;
            if (lookTargets == null)
                return dto;

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
            return dto;
        }
    }
}
