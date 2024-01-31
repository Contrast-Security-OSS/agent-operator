// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting.Patching.Agents
{
    public class PythonPatcherTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void GenerateEnvVars_should_not_return_rewrite_if_not_rewriting()
        {
            var patcher = new PythonAgentPatcher(new InjectorOptions(false, false));
            var context = AutoFixture.Create<PatchingContext>();
            var expectedEnvVars = new List<string>
            {
                "PYTHONPATH",
                "__CONTRAST_USING_RUNNER",
                "CONTRAST__AGENT__LOGGER__PATH"
            };

            // Act
            var result = patcher.GenerateEnvVars(context).ToList();

            // Assert
            result.Should().Equal(expectedEnvVars, (source, expected) => source.Name == expected);
        }


        [Fact]
        public void GenerateEnvVars_should_return_rewrite_if_rewriting()
        {
            var patcher = new PythonAgentPatcher(new InjectorOptions(false, true));
            var context = AutoFixture.Create<PatchingContext>();
            var expectedEnvVars = new List<string>
            {
                "PYTHONPATH",
                "CONTRAST__AGENT__PYTHON__REWRITE",
                "__CONTRAST_USING_RUNNER",
                "CONTRAST__AGENT__LOGGER__PATH"
            };

            // Act
            var result = patcher.GenerateEnvVars(context).ToList();

            // Assert
            result.Should().Equal(expectedEnvVars, (source, expected) => source.Name == expected);
        }
    }
}
