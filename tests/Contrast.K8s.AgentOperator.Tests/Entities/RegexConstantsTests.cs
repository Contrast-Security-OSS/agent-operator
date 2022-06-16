// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Contrast.K8s.AgentOperator.Entities;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Entities
{
    public class RegexConstantsTests
    {
        [Theory]
        [InlineData("dotnet-core")]
        [InlineData("dotnet")]
        [InlineData("java")]
        [InlineData("node")]
        [InlineData("nodejs")]
        [InlineData("php")]
        [InlineData("personal-home-page")]
        [InlineData("dummy")]
        public void AgentTypeRegex_should_match_valid_values(string input)
        {
            const string regex = RegexConstants.AgentTypeRegex;

            // Act
            var result = Regex.IsMatch(input, regex);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("latest")]
        [InlineData("2")]
        [InlineData("2.10")]
        [InlineData("2.10.20")]
        [InlineData("2.10.20-foo")]
        [InlineData("2.10.20.30")]
        [InlineData("2.10.20.30-foo")]
        public void InjectorVersion_should_match_valid_values(string input)
        {
            const string regex = RegexConstants.InjectorVersionRegex;

            // Act
            var result = Regex.IsMatch(input, regex);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("Always")]
        [InlineData("IfNotPresent")]
        [InlineData("Never")]
        public void PullPolicyRegex_should_match_valid_values(string input)
        {
            const string regex = RegexConstants.PullPolicyRegex;

            // Act
            var result = Regex.IsMatch(input, regex);

            // Assert
            result.Should().BeTrue();
        }
    }
}
