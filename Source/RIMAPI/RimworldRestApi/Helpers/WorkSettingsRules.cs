namespace RIMAPI.Helpers
{
    // The three states a pawn's work settings can be in (fox ticket #3566).
    // Pawn_WorkSettings.EverWork is the game's own answer to "were the
    // priorities ever created": it is true exactly while the private
    // DefMap<WorkTypeDef, int> exists, which is what the work-priorities read
    // reports as `initialized`. A pawn in the middle state has a settings
    // component but no levels, so every work type reads 0 and the work tab is
    // empty; the game still lets it be written, because
    // EnableAndInitializeIfNotAlreadyInitialized() is its own lazy path and
    // fills the defaults on the first write. The rules stay free of game types
    // so Source/RIMAPI.Tests can state them; the calls live in
    // ColonistService.GetColonistWorkPriorities and SetColonistWorkPriority.
    public static class WorkSettingsRules
    {
        public enum Readiness
        {
            // No Pawn_WorkSettings component at all: nothing to read or write.
            NoSettings,
            // The component exists but its priorities were never created.
            NeedsInitialization,
            // The priorities exist; the read can list them.
            Ready,
        }

        public static Readiness Of(bool hasWorkSettings, bool everWork)
        {
            if (!hasWorkSettings) return Readiness.NoSettings;
            return everWork ? Readiness.Ready : Readiness.NeedsInitialization;
        }

        // Name is the state as the read sends it (fox #3604). `initialized`
        // alone is false for both NoSettings and NeedsInitialization, and the
        // two need different advice: only the second can be initialized by a
        // write, the first is refused by the game. An older tool that does not
        // know the field still reads `initialized` and keeps its old wording.
        public static string Name(Readiness readiness)
        {
            switch (readiness)
            {
                case Readiness.NoSettings:
                    return "no_settings";
                case Readiness.NeedsInitialization:
                    return "needs_initialization";
                default:
                    return "ready";
            }
        }

        // NoSettingsRefusal names the pawn, so a run does not have to guess
        // which of its colonists the refusal was about.
        public static string NoSettingsRefusal(string pawnLabel)
        {
            return $"Pawn {pawnLabel} has no work settings, so it can take no work priority";
        }
    }
}
