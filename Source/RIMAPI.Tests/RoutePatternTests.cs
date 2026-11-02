using System.Text.RegularExpressions;
using RIMAPI.Core;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The route pattern to regex conversion (fox ticket #4234): a broken
    /// conversion is invisible until a route with a parameter is registered,
    /// and then it takes the rest of its controller's routes with it.
    /// </summary>
    public class RoutePatternTests
    {
        [Fact]
        public void AStaticPatternMatchesOnlyItself()
        {
            var regex = new Regex(RoutePattern.ToRegex("/api/v1/docs/health"));

            Assert.Matches(regex, "/api/v1/docs/health");
            Assert.DoesNotMatch(regex, "/api/v1/docs/health/extra");
            Assert.DoesNotMatch(regex, "/api/v1/docsXhealth");
        }

        [Fact]
        public void ABracedParameterBecomesACapturedGroup()
        {
            var regex = new Regex(RoutePattern.ToRegex("/api/v1/docs/extensions/{extensionId}"));

            Match match = regex.Match("/api/v1/docs/extensions/builder");

            Assert.True(match.Success);
            Assert.Equal("builder", match.Groups["param_extensionId"].Value);
            Assert.DoesNotMatch(regex, "/api/v1/docs/extensions/");
            Assert.DoesNotMatch(regex, "/api/v1/docs/extensions/a/b");
        }

        [Fact]
        public void AColonParameterIsTheSameThing()
        {
            var regex = new Regex(RoutePattern.ToRegex("/api/v1/things/:id"));

            Match match = regex.Match("/api/v1/things/42");

            Assert.True(match.Success);
            Assert.Equal("42", match.Groups["param_id"].Value);
        }

        [Fact]
        public void AParameterInTheMiddleKeepsTheRestLiteral()
        {
            var regex = new Regex(RoutePattern.ToRegex("/api/v1/maps/{mapId}/things.json"));

            Assert.Matches(regex, "/api/v1/maps/7/things.json");
            Assert.DoesNotMatch(regex, "/api/v1/maps/7/thingsXjson");
        }
    }
}
