namespace RIMAPI.Helpers
{
    /// <summary>
    /// A frame whose building needs no work at all cannot be finished by the
    /// game: the finish-frame driver rolls a construction failure with
    /// 1 - ConstructSuccessChance^(work / WorkToBuild), and a zero WorkToBuild
    /// makes that exponent infinite, so the roll is 1 on every tick. The frame
    /// is destroyed and the blueprint placed again, forever, and a def such as
    /// AnimalSleepingSpot (WorkToBuild 0, no material) can never exist
    /// (fox ticket #3610). Such a frame is finished instead of failed.
    ///
    /// The rule lives here, away from the game types, so a unit test can pin it
    /// (see FrameConstructionTests).
    /// </summary>
    public static class FrameConstruction
    {
        /// <summary>
        /// Whether a frame that the game wants to fail should be completed
        /// instead. Without a worker there is nobody to credit for the work, so
        /// the game's own path stands.
        /// </summary>
        public static bool FinishInsteadOfFailing(float workToBuild, bool workerMissing)
        {
            return !workerMissing && workToBuild <= 0f;
        }

        /// <summary>
        /// The log line the hook writes when it finishes such a frame, so the
        /// log names the def and the work value the rule read.
        /// </summary>
        public static string LogLine(string buildDef, float workToBuild)
        {
            return $"finished a zero-work frame instead of failing it: {buildDef} needs {workToBuild} work";
        }
    }
}
