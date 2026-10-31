namespace RIMAPI.Models
{
    // One entry of the game's own message archive (ticket #3336). The little
    // notifications land there and in the save file's <history><archive>, so
    // until this read a caller only saw them by saving the colony and parsing
    // the file. Class is always a plain Message here: letters have their own
    // read, and a death letter is a letter.
    //
    // The target is what the message is about: a plant that died of cold, the
    // pawn being attacked, the cell a blight struck. A message the game sent
    // without a target leaves HasTarget false.
    public class MessageDto
    {
        // The MessageTypeDef defName: NegativeEvent, ThreatSmall, ThreatBig,
        // PositiveEvent, SilentInput, ...
        public string Def { get; set; }
        // The game's own text, e.g. "Rice plant has died because of cold."
        public string Text { get; set; }
        // The game tick the message was created, the same clock the game state
        // reads.
        public int Tick { get; set; }
        public bool HasTarget { get; set; }
        // The target thing's label ("Rice plant") when the message names one;
        // empty when the target is only a cell.
        public string TargetLabel { get; set; }
        public string TargetDef { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        // The map the target stands on; 0 is the home map.
        public int MapId { get; set; }
    }
}
