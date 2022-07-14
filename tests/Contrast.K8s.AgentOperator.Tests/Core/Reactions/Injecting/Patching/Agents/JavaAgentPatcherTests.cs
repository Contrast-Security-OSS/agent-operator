// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using FluentAssertions;
using k8s.Models;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting.Patching.Agents
{
    public class JavaAgentPatcherTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void Type_should_return_correct_type()
        {
            var patcher = new JavaAgentPatcher();

            // Act
            var result = patcher.Type;

            // Assert
            result.Should().Be(AgentInjectionType.Java);
        }

        [Fact]
        public void GetMountPath_should_return_correct_path()
        {
            var patcher = new JavaAgentPatcher();

            // Act
            var result = patcher.GetMountPath();

            // Assert
            result.Should().Be("/opt/contrast");
        }

        [Fact]
        public void GenerateEnvVars_should_return_correct_env_vars()
        {
            var contextFake = AutoFixture.Create<PatchingContext>();
            var patcher = new JavaAgentPatcher();

            // Act
            var result = patcher.GenerateEnvVars(contextFake);

            // Assert
            result.Should().BeEquivalentTo(new List<V1EnvVar>
            {
                new("JAVA_TOOL_OPTIONS", $"-javaagent:{contextFake.ContrastMountPath}/contrast-agent.jar")
            });
        }
    }
}
