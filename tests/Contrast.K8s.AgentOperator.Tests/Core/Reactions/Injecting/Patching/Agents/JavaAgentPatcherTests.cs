// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using FluentAssertions;
using FluentAssertions.Execution;
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
            var result = patcher.GetOverrideAgentMountPath();

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
                new("JAVA_TOOL_OPTIONS", $"-javaagent:{contextFake.AgentMountPath}/contrast-agent.jar"),
                new("CONTRAST__AGENT__CONTRAST_WORKING_DIR", contextFake.WritableMountPath),
                new("CONTRAST__AGENT__LOGGER__PATH", $"{contextFake.WritableMountPath}/logs/contrast_agent.log"),
                new("CONTRAST_INSTALL_SOURCE", "kubernetes-operator"),
            });
        }

        [Fact]
        public void PatchContainer_should_not_do_anything_if_we_already_injected()
        {
            var patcher = new JavaAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var existingToolOptions = "-javaagent:/somepath/contrast-agent.jar";
            var container = AutoFixture.Build<V1Container>()
                .With(x => x.Env, new List<V1EnvVar> { new("JAVA_TOOL_OPTIONS", existingToolOptions) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            using (new AssertionScope())
            {
                container.Env.Should().NotContain(x => x.Name == "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS");
                container.Env.Should().Contain(x => x.Name == "JAVA_TOOL_OPTIONS" && x.Value == existingToolOptions);
            }
        }

        [Fact]
        public void PatchContainer_should_handle_existing_javaagent()
        {
            var patcher = new JavaAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var existingToolOptions = "-javaagent:/somepath/some-tool.jar -Dcontrast.dir=/tmp";
            var container = AutoFixture.Build<V1Container>()
                .With(x => x.Env, new List<V1EnvVar> { new("JAVA_TOOL_OPTIONS", existingToolOptions) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            var expectedToolOptions = $"-javaagent:{context.AgentMountPath}/contrast-agent.jar -javaagent:/somepath/some-tool.jar -Dcontrast.dir=/tmp";
            using (new AssertionScope())
            {
                container.Env.Should()
                    .Contain(x => x.Name == "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS" && x.Value == existingToolOptions);
                container.Env.Should()
                    .Contain(x => x.Name == "JAVA_TOOL_OPTIONS" && x.Value == expectedToolOptions);
            }
        }

        [Fact]
        public void PatchContainer_should_handle_existing_correct_contrast_javaagent()
        {
            var patcher = new JavaAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var existingToolOptions = "-javaagent:/somepath/contrast-agent.jar -Dcontrast.dir=/tmp";
            var container = AutoFixture.Build<V1Container>()
                .With(x => x.Env, new List<V1EnvVar> { new("JAVA_TOOL_OPTIONS", existingToolOptions) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            var expectedToolOptions = $"-javaagent:{context.AgentMountPath}/contrast-agent.jar -Dcontrast.dir=/tmp";
            using (new AssertionScope())
            {
                container.Env.Should()
                    .Contain(x => x.Name == "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS" && x.Value == existingToolOptions);
                container.Env.Should()
                    .Contain(x => x.Name == "JAVA_TOOL_OPTIONS" && x.Value == expectedToolOptions);
            }
        }

        [Fact]
        public void PatchContainer_should_handle_no_javaagent()
        {
            var patcher = new JavaAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var existingToolOptions = "-Dcontrast.dir=/tmp";
            var container = AutoFixture.Build<V1Container>()
                .With(x => x.Env, new List<V1EnvVar> { new("JAVA_TOOL_OPTIONS", existingToolOptions) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            var expectedToolOptions = $"-javaagent:{context.AgentMountPath}/contrast-agent.jar -Dcontrast.dir=/tmp";
            using (new AssertionScope())
            {
                container.Env.Should()
                    .Contain(x => x.Name == "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS" && x.Value == existingToolOptions);
                container.Env.Should()
                    .Contain(x => x.Name == "JAVA_TOOL_OPTIONS" && x.Value == expectedToolOptions);
            }
        }

        [Fact]
        public void PatchContainer_should_not_patch_on_malformed_options()
        {
            var patcher = new JavaAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var existingToolOptions = "-Dcontrast.dir='/tmp";
            var container = AutoFixture.Build<V1Container>()
                .With(x => x.Env, new List<V1EnvVar> { new("JAVA_TOOL_OPTIONS", existingToolOptions) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            using (new AssertionScope())
            {
                container.Env.Should()
                    .Contain(x => x.Name == "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS" && x.Value == existingToolOptions);
                container.Env.Should()
                    .Contain(x => x.Name == "JAVA_TOOL_OPTIONS" && x.Value == existingToolOptions);
            }
        }

    }
}
