using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using RIMAPI.Core;
using Xunit;

namespace RIMAPI.Tests
{
    // The generic response filter (SendJsonResponse<T>) sees the whole query
    // string, so it also sees the parameters a route reads for itself: limit,
    // since, archive. Filtering on one of those drops every item, which is how
    // the archive reads answered an empty list (ticket #3439). A key that names
    // no field of the item type is not a filter and must leave the list alone.
    public class QueryFilterTests
    {
        private class Entry
        {
            public string Class { get; set; }
            public int Tick { get; set; }
            public Detail Detail { get; set; }
        }

        private class Detail
        {
            public string Name { get; set; }
        }

        private class Holder
        {
            public List<Entry> Entries { get; set; }
        }

        private static NameValueCollection Query(params string[] pairs)
        {
            var query = new NameValueCollection();
            for (var i = 0; i + 1 < pairs.Length; i += 2)
                query.Add(pairs[i], pairs[i + 1]);
            return query;
        }

        private static List<Entry> Entries()
        {
            return new List<Entry>
            {
                new Entry { Class = "Message", Tick = 10, Detail = new Detail { Name = "a" } },
                new Entry { Class = "StandardLetter", Tick = 20, Detail = new Detail { Name = "b" } },
            };
        }

        [Theory]
        [InlineData("limit", "25")]
        [InlineData("since", "0")]
        [InlineData("archive", "1")]
        [InlineData("mark_seen", "true")]
        public void ARouteParameterKeepsEveryEntry(string key, string value)
        {
            Assert.Equal(2, QueryFilter.Apply(Entries(), Query(key, value)).Count());
        }

        [Fact]
        public void AFieldFilterStillFilters()
        {
            var kept = QueryFilter.Apply(Entries(), Query("class", "Message")).ToList();

            Assert.Single(kept);
            Assert.Equal("Message", kept[0].Class);
        }

        [Fact]
        public void AFieldFilterThatMatchesNothingAnswersNothing()
        {
            Assert.Empty(QueryFilter.Apply(Entries(), Query("class", "Raid")));
        }

        [Fact]
        public void AMixedQueryFiltersOnlyOnItsField()
        {
            var kept = QueryFilter.Apply(Entries(), Query("limit", "25", "class", "StandardLetter")).ToList();

            Assert.Single(kept);
            Assert.Equal("StandardLetter", kept[0].Class);
        }

        [Fact]
        public void ANestedFilterUsesItsHeadField()
        {
            Assert.Single(QueryFilter.Apply(Entries(), Query("detail.name", "a")));
        }

        [Fact]
        public void ANestedFilterOnAnUnknownHeadIsIgnored()
        {
            Assert.Equal(2, QueryFilter.Apply(Entries(), Query("other.name", "a")).Count());
        }

        [Fact]
        public void TheResultBranchLeavesARouteParameterAlone()
        {
            var holder = new Holder { Entries = Entries() };

            var filtered = (Holder)QueryFilter.ApplyToResult(holder, Query("entries.limit", "5"));

            Assert.Equal(2, filtered.Entries.Count);
        }

        [Fact]
        public void TheResultBranchStillFiltersOnAField()
        {
            var holder = new Holder { Entries = Entries() };

            var filtered = (Holder)QueryFilter.ApplyToResult(holder, Query("entries.class", "Message"));

            Assert.Single(filtered.Entries);
        }

        [Fact]
        public void TheThingsDefsFallbackLeavesARouteParameterAlone()
        {
            // The fallback hands every unprefixed key to a `things_defs`
            // collection, route parameters included.
            var holder = new ThingsHolder { ThingsDefs = Entries() };

            var filtered = (ThingsHolder)QueryFilter.ApplyToResult(holder, Query("limit", "5"));

            Assert.Equal(2, filtered.ThingsDefs.Count);
        }

        private class ThingsHolder
        {
            public List<Entry> ThingsDefs { get; set; }
        }
    }
}
