using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Reactions;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting
{
    public class GlobMatcherTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void Given_any_string_then_splat_should_match()
        {
            var matcher = new GlobMatcher();

            // Act
            var result = matcher.Matches("*", AutoFixture.Create<string>());

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Given_a_prefix_string_then_prefix_splat_should_match()
        {
            var matcher = new GlobMatcher();

            // Act
            var result = matcher.Matches("foo*", "foo" + AutoFixture.Create<string>());

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Given_a_suffix_string_then_suffix_splat_should_match()
        {
            var matcher = new GlobMatcher();

            // Act
            var result = matcher.Matches("*foo", AutoFixture.Create<string>() + "foo");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Given_a_string_then_any_case_should_match()
        {
            var matcher = new GlobMatcher();

            // Act
            var result = matcher.Matches("foo", "FOO");

            // Assert
            result.Should().BeTrue();
        }
    }
}
