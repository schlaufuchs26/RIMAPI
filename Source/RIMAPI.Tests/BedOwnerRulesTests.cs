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
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Neeko", Id = 12 });
            Assert.Null(BedOwnerRules.Problem(state));
        }

        [Fact]
        public void ADoubleBedTakesASecondColonist()
        {
            // A double bed has two slots (fox ticket #3874): one owner must not
            // read as "full", or the second colonist of a couple can never be
            // assigned and the bedroom stays half empty.
            var state = ColonistOnFreeBed();
            state.Capacity = 2;
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Danil", Id = 4 });
            Assert.Null(BedOwnerRules.Problem(state));
        }

        [Fact]
        public void ADoubleBedWithTwoOwnersIsFull()
        {
            var state = ColonistOnFreeBed();
            state.Capacity = 2;
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Danil", Id = 4 });
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Val", Id = 5 });
            string problem = BedOwnerRules.Problem(state);
            Assert.Contains("Danil (id 4)", problem);
            Assert.Contains("Val (id 5)", problem);
            Assert.Contains("clear one owner first", problem);
        }

        [Fact]
        public void ASecondOwnerOfADoubleBedMayKeepHisSlot()
        {
            // The second slot's owner asking again for his own bed changes
            // nothing; the rule must not read him as a third colonist.
            var state = ColonistOnFreeBed();
            state.Capacity = 2;
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Danil", Id = 4 });
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Neeko", Id = 12 });
            Assert.Null(BedOwnerRules.Problem(state));
        }

        [Fact]
        public void ACapacityTheBridgeDidNotReadCountsAsOneSlot()
        {
            // Capacity 0 is what a bed reports when nothing filled it in; the
            // safe reading is a single slot, never "unlimited".
            var state = ColonistOnFreeBed();
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Squint", Id = 44 });
            Assert.Contains("clear the owner first", BedOwnerRules.Problem(state));
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
            state.Owners.Add(new BedOwnerRules.Owner { Name = "Squint", Id = 44 });
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
