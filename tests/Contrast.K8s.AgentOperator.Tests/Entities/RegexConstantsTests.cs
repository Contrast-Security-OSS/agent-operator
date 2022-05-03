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
        [InlineData("java")]
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
        public void InjectorVersion_should_match_valid_values(string input)
        {
            const string regex = RegexConstants.InjectorVersionRegex;

            // Act
            var result = Regex.IsMatch(input, regex);

            // Assert
            result.Should().BeTrue();
        }
    }
}
