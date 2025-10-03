// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting.Patching.Agents
{
    public class PythonAgentPatcherTests
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
                "CONTRAST__AGENT__LOGGER__PATH",
                "CONTRAST_INSTALLATION_TOOL",
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
                "CONTRAST__AGENT__LOGGER__PATH",
                "CONTRAST_INSTALLATION_TOOL",
            };

            // Act
            var result = patcher.GenerateEnvVars(context).ToList();

            // Assert
            result.Should().Equal(expectedEnvVars, (source, expected) => source.Name == expected);
        }

        [Fact]
        public void PatchContainer_should_add_pythonpath_if_already_set()
        {
            var patcher = new PythonAgentPatcher(new InjectorOptions(false, false));
            var context = AutoFixture.Create<PatchingContext>();
            var existingPath = AutoFixture.Create<string>();
            var container = AutoFixture.Build<V1Container>()
                                       .With(x => x.Env, new List<V1EnvVar> { new("PYTHONPATH", existingPath) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            using (new AssertionScope())
            {
                container.Env.Should()
                         .Contain(x => x.Name == "CONTRAST_EXISTING_PYTHONPATH" && x.Value == existingPath);
                container.Env.Should().Contain(x =>
                    x.Name == "PYTHONPATH" && x.Value ==
                    $"{context.AgentMountPath}:{context.AgentMountPath}/contrast/loader:{existingPath}");
            }
        }

        [Fact]
        public void PatchContainer_should_not_do_anything_if_we_already_injected()
        {
            var patcher = new PythonAgentPatcher(new InjectorOptions(true, false));
            var context = AutoFixture.Create<PatchingContext>();
            var existingPath = AutoFixture.Create<string>() + "/contrast/loader";
            var container = AutoFixture.Build<V1Container>()
                .With(x => x.Env, new List<V1EnvVar> { new("PYTHONPATH", existingPath) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            using (new AssertionScope())
            {
                container.Env.Should().NotContain(x => x.Name == "CONTRAST_EXISTING_PYTHONPATH");
                container.Env.Should().Contain(x => x.Name == "PYTHONPATH" && x.Value == existingPath);
            }
        }

    }
}
