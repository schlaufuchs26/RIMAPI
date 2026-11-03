using RIMAPI.Core;

namespace RIMAPI.Models
{
    // One entry of the game's own message archive (ticket #3336). The little
    // notifications land there and in the save file's <history><archive>, so
    // until this read a caller only saw them by saving the colony and parsing
    // the file.
    //
    // The archive holds letters too, and a death letter is where the cause of a
    // death is written, so this read answers them as well: Class says which
    // kind an entry is (the save writes the same name as its Class attribute),
    // and Label is a letter's title (ticket #3400). A caller that only wants
    // the little notifications filters on Class = "Message".
    //
    // The target is what the message is about: a plant that died of cold, the
    // pawn being attacked, the cell a blight struck. A message the game sent
    // without a target leaves HasTarget false.
    public class MessageDto
    {
        // The entry's class as the save writes it: Message for a notification,
        // StandardLetter / DeathLetter / ChoiceLetter_... for a letter.
        [ParameterDescription("The entry's class as the save writes it: Message for a notification, StandardLetter / DeathLetter / ChoiceLetter_...")]
        public string Class { get; set; }
        // The MessageTypeDef defName: NegativeEvent, ThreatSmall, ThreatBig,
        // PositiveEvent, SilentInput, ...
        [ParameterDescription("The MessageTypeDef defName: NegativeEvent, ThreatSmall, ThreatBig, PositiveEvent, SilentInput, ...")]
        public string Def { get; set; }
        // A letter's title ("Visitors", "Death: Vulpes"); empty for a
        // notification, which has only its text.
        public string Label { get; set; }
        // The game's own text, e.g. "Rice plant has died because of cold."
        [ParameterDescription("The game's own text, e.g.")]
        public string Text { get; set; }
        // The game tick the entry was created (a letter: when it arrived), the
        // same clock the game state reads.
        [ParameterDescription("The game tick the entry was created (a letter: when it arrived), the same clock the game state reads.")]
        public int Tick { get; set; }
        [ParameterDescription("Whether the message points at a thing.")]
        public bool HasTarget { get; set; }
        // The target thing's label ("Rice plant") when the message names one;
        // empty when the target is only a cell.
        [ParameterDescription("The target thing's label (\"Rice plant\") when the message names one; empty when the target is only a cell.")]
        public string TargetLabel { get; set; }
        [ParameterDescription("The def of the thing it points at.")]
        public string TargetDef { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        // The map the target stands on; 0 is the home map.
        public int MapId { get; set; }
    }
}
