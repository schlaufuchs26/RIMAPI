using System;
using System.Threading;
using System.Threading.Tasks;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The dev-action menu is built once per game (fox ticket #3946): the walk
    // costs about 8 s for 60,456 entries, so a second filter must not pay it
    // again, and a colony change must drop the old menu.
    public class MenuCacheTests
    {
        private sealed class Entry
        {
            public Entry(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        [Fact]
        public void TwoReadsBuildTheMenuOnce()
        {
            var cache = new MenuCache<Entry>();
            int builds = 0;
            Func<Entry> build = () =>
            {
                builds++;
                return new Entry("menu");
            };

            Entry first = cache.Get(build);
            Entry second = cache.Get(build);

            Assert.Same(first, second);
            Assert.Equal(1, builds);
            Assert.Equal(1, cache.Builds);
            Assert.True(cache.Holds);
        }

        [Fact]
        public void ADroppedMenuIsBuiltAgain()
        {
            var cache = new MenuCache<Entry>();
            int builds = 0;
            Func<Entry> build = () =>
            {
                builds++;
                return new Entry("menu " + builds);
            };

            Entry first = cache.Get(build);
            cache.Invalidate();

            Assert.False(cache.Holds);

            Entry second = cache.Get(build);

            Assert.NotSame(first, second);
            Assert.Equal("menu 2", second.Name);
            Assert.Equal(2, cache.Builds);
        }

        [Fact]
        public void ACallerThatArrivesDuringTheBuildWaitsForIt()
        {
            var cache = new MenuCache<Entry>();
            int builds = 0;
            Func<Entry> build = () =>
            {
                Interlocked.Increment(ref builds);
                Thread.Sleep(50);
                return new Entry("menu");
            };

            var seen = new Entry[8];
            Parallel.For(0, seen.Length, i => seen[i] = cache.Get(build));

            Assert.Equal(1, builds);
            foreach (Entry entry in seen)
            {
                Assert.Same(seen[0], entry);
            }
        }

        [Fact]
        public void ANullBuilderIsRefused()
        {
            var cache = new MenuCache<Entry>();

            Assert.Throws<ArgumentNullException>(() => cache.Get(null));
        }
    }
}
