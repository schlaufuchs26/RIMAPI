using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimworldRestApi.Hooks;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// Which OpenLetter implementations the letter hook patches (ticket #3318).
    /// The types below are a small stand-in for the game's letter hierarchy:
    /// Verse.Letter declares OpenLetter abstract, Verse.LetterWithTimeout is an
    /// abstract type in between that implements nothing, ChoiceLetter
    /// implements it once and two concrete letters inherit that.
    /// </summary>
    public class LetterOpenTargetsTests
    {
        [Fact]
        public void An_abstract_type_that_implements_nothing_is_not_a_target()
        {
            // The bug: the hook named Verse.LetterWithTimeout, which declares no
            // OpenLetter, so Harmony refused the patch on every game start.
            LetterOpenScan scan = ScanAll();

            Assert.DoesNotContain(scan.Targets, m => m.DeclaringType == typeof(FakeLetterWithTimeout));
            Assert.Empty(scan.Problems);
        }

        [Fact]
        public void A_concrete_letter_that_implements_itself_is_a_target()
        {
            LetterOpenScan scan = ScanAll();

            MethodInfo bundle = Assert.Single(
                scan.Targets, m => m.DeclaringType == typeof(FakeBundleLetter));
            Assert.Equal(LetterOpenTargets.MethodName, bundle.Name);
        }

        [Fact]
        public void An_inherited_implementation_is_patched_once_not_per_subclass()
        {
            // FakeDeathLetter and FakeQuestLetter inherit ChoiceLetter's
            // OpenLetter; patching each subclass would mark one letter seen
            // twice, so the method is the target, not the type.
            LetterOpenScan scan = ScanAll();

            Assert.Single(scan.Targets, m => m.DeclaringType == typeof(FakeChoiceLetter));
            Assert.Equal(2, scan.Targets.Count);
        }

        [Fact]
        public void A_type_listed_twice_yields_one_target()
        {
            // Two assemblies can hand the same type in; a second Harmony patch on
            // the same method would run the postfix twice.
            var types = new List<Type> { typeof(FakeBundleLetter), typeof(FakeBundleLetter) };

            LetterOpenScan scan = LetterOpenTargets.Scan(typeof(FakeLetter), types);

            Assert.Single(scan.Targets);
        }

        [Fact]
        public void A_missing_letter_type_is_a_problem_not_an_exception()
        {
            LetterOpenScan scan = LetterOpenTargets.Scan(null, new[] { typeof(FakeBundleLetter) });

            Assert.Empty(scan.Targets);
            Assert.Single(scan.Problems);
        }

        [Fact]
        public void An_assembly_that_lists_no_types_costs_a_problem_not_the_hook()
        {
            // null stands in for a type a partially loadable assembly could not
            // hand over.
            var types = new List<Type> { null, typeof(FakeBundleLetter) };

            LetterOpenScan scan = LetterOpenTargets.Scan(typeof(FakeLetter), types);

            Assert.Single(scan.Targets);
            Assert.Empty(scan.Problems);
        }

        private static LetterOpenScan ScanAll()
        {
            var types = new List<Type>
            {
                typeof(FakeLetter),
                typeof(FakeLetterWithTimeout),
                typeof(FakeChoiceLetter),
                typeof(FakeDeathLetter),
                typeof(FakeQuestLetter),
                typeof(FakeBundleLetter),
                typeof(string),
            };
            return LetterOpenTargets.Scan(typeof(FakeLetter), types);
        }

        private abstract class FakeLetter
        {
            public abstract void OpenLetter();
        }

        private abstract class FakeLetterWithTimeout : FakeLetter
        {
            // Verse.LetterWithTimeout holds a timeout and no OpenLetter.
            public int disappearAtTick = -1;
        }

        private abstract class FakeChoiceLetter : FakeLetterWithTimeout
        {
            public override void OpenLetter() { }
        }

        private class FakeDeathLetter : FakeChoiceLetter { }

        private class FakeQuestLetter : FakeChoiceLetter { }

        private class FakeBundleLetter : FakeLetter
        {
            public override void OpenLetter() { }
        }
    }
}
