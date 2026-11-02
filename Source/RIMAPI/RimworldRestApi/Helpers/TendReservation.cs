namespace RIMAPI.Helpers
{
    /// <summary>
    /// Why an ordered tend cannot start: another pawn already holds the
    /// patient's reservation (fox ticket #4167). JobDriver_TendPatient's
    /// TryMakePreToilReservations reserves the patient with maxPawns 1 and ends
    /// the job when it cannot, so the order was accepted and the job vanished
    /// on its first tick. The mod answered "Failed to assign job", which reads
    /// like the doctor's own problem; naming the pawn that holds the patient
    /// and the job it holds him for is the half a run can act on.
    /// </summary>
    public static class TendReservation
    {
        /// <summary>
        /// The whole sentence a tend is refused with, or null when nobody else
        /// holds the patient. <paramref name="holderJob"/> is the defName of
        /// the job the reservation belongs to, empty when the game reports
        /// none.
        /// </summary>
        public static string Refusal(
            bool selfTend,
            string patientName,
            int patientId,
            string holderName,
            int holderId,
            string holderJob
        )
        {
            if (string.IsNullOrEmpty(holderName)) return null;
            string patient = selfTend
                ? $"Pawn {patientName} ({patientId})"
                : $"Patient {patientName} ({patientId})";
            string job = string.IsNullOrEmpty(holderJob) ? "another job" : holderJob;
            return $"{patient} is reserved by {holderName} ({holderId}) for {job}, so the tend job would "
                + "die on its first tick: that job has to end before this one can be queued";
        }
    }
}
