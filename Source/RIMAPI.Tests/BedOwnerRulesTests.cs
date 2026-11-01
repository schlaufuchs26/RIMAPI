using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The rules behind a bed's owner (fox ticket #3771): a colonist takes a
    // free humanlike bed, and every other combination is refused with the fact
    // that stops it rather than a bare "failed".
    public class BedOwnerRulesTests
    {
        private static BedOwnerRules.State ColonistOnFreeBed()
        {
            return new BedOwnerRules.State
            {
                BedIsHumanlike = true,
                Pawn = "Neeko",
                PawnId = 12,
                PawnIsColonist = true,
            };
        }

        [Fact]
        public void AColonistTakesAFreeHumanlikeBed()
        {
            Assert.Null(BedOwnerRules.Problem(ColonistOnFreeBed()));
        }

        [Fact]
        public void AColonistMayKeepTheBedHeAlreadyOwns()
        {
            // The bed tab shows the owner his own bed; a second call for the
            // same pawn is no refusal, it changes nothing.
            var state = ColonistOnFreeBed();
            state.BedOwner = "Neeko";
            state.BedOwnerId = 12;
            Assert.Null(BedOwnerRules.Problem(state));
        }

        [Fact]
        public void AnAnimalBedTakesNoColonist()
        {
            var state = ColonistOnFreeBed();
            state.BedIsHumanlike = false;
            string problem = BedOwnerRules.Problem(state);
            Assert.Contains("humanlike", problem);
        }

        [Fact]
        public void APrisonerBedIsThePrisoners()
        {
            var state = ColonistOnFreeBed();
            state.BedForPrisoners = true;
            string problem = BedOwnerRules.Problem(state);
            Assert.Contains("prisoners", problem);
            Assert.Contains("bed_for=colonists", problem);
        }

        [Fact]
        public void ASlaveBedIsTheSlaves()
        {
            var state = ColonistOnFreeBed();
            state.BedForSlaves = true;
            Assert.Contains("slaves", BedOwnerRules.Problem(state));
        }

        [Theory]
        [InlineData("a prisoner")]
        [InlineData("a guest")]
        [InlineData("an animal")]
        [InlineData("a wild man")]
        public void OnlyAColonistTakesABed(string kind)
        {
            var state = ColonistOnFreeBed();
            state.PawnIsColonist = false;
            state.PawnKind = kind;
            string problem = BedOwnerRules.Problem(state);
            Assert.Contains("Neeko (id 12)", problem);
            Assert.Contains(kind, problem);
        }

        [Fact]
        public void ABedThatBelongsToAnotherColonistIsNotTakenOver()
        {
            // The answer names the owner and his id, so the caller can clear
            // that bed or pick another one; taking it over silently would leave
            // a colonist on a bed no read explains any more.
            var state = ColonistOnFreeBed();
            state.BedOwner = "Squint";
            state.BedOwnerId = 44;
            string problem = BedOwnerRules.Problem(state);
            Assert.Contains("Squint (id 44)", problem);
            Assert.Contains("clear the owner first", problem);
        }

        [Fact]
        public void APawnWithoutANameIsNamedByHisId()
        {
            var state = ColonistOnFreeBed();
            state.Pawn = "";
            state.PawnIsColonist = false;
            Assert.Contains("pawn 12", BedOwnerRules.Problem(state));
        }
    }
}
