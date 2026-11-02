using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #4167: the ordered tend died on its first tick when another pawn
    // held the patient's reservation, and the refusal named only the doctor.
    // These state the sentence a run gets instead.
    public class TendReservationTests
    {
        [Fact]
        public void NobodyHoldingThePatientIsNoRefusal()
        {
            Assert.Null(TendReservation.Refusal(false, "Arianites", 1063, null, 0, null));
            Assert.Null(TendReservation.Refusal(false, "Arianites", 1063, "", 0, null));
        }

        [Fact]
        public void TheRefusalNamesTheHolderAndTheJobItHoldsThePatientFor()
        {
            string why = TendReservation.Refusal(false, "Arianites", 1063, "Mangle", 857, "TendPatient");
            Assert.Contains("Patient Arianites (1063)", why);
            Assert.Contains("reserved by Mangle (857)", why);
            Assert.Contains("for TendPatient", why);
        }

        // A reservation without a job def is still the blocker, and the
        // sentence must not read as if the game named one.
        [Fact]
        public void AReservationWithoutAJobSaysSo()
        {
            string why = TendReservation.Refusal(false, "Arianites", 1063, "Mangle", 857, null);
            Assert.Contains("reserved by Mangle (857)", why);
            Assert.Contains("for another job", why);
        }

        // Self-tend is the same gate with the doctor as the patient, and the
        // old message ("its own reservation is held by something else") named
        // neither the holder nor the job.
        [Fact]
        public void ASelfTendRefusalCallsThePawnAPawn()
        {
            string why = TendReservation.Refusal(true, "Tilger", 857, "Odette", 904, "HaulToCell");
            Assert.Contains("Pawn Tilger (857)", why);
            Assert.DoesNotContain("Patient Tilger", why);
            Assert.Contains("for HaulToCell", why);
        }
    }
}
