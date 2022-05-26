using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Injecting.Patching;
using Contrast.K8s.AgentOperator.Core.Injecting.Patching.Agents;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Injecting
{
    public class DotNetPatcherTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void GenerateEnvVars_should_return_profiling_vars()
        {
            var patcher = new DotNetAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var expectedEnvVars = new List<string>
            {
                "CORECLR_PROFILER",
                "CORECLR_PROFILER_PATH",
                "CORECLR_ENABLE_PROFILING",
                "CONTRAST_SOURCE",
                "CONTRAST_CORECLR_INSTALL_DIRECTORY",
            };

            // Act
            var result = patcher.GenerateEnvVars(context).ToList();

            // Assert
            result.Should().Equal(expectedEnvVars, (source, expected) => source.Name == expected);
        }

        [Fact]
        public void PatchContainer_should_add_ld_preload_if_already_set()
        {
            var patcher = new DotNetAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var existingPreload = AutoFixture.Create<string>();
            var container = AutoFixture.Build<V1Container>()
                .With(x => x.Env, new List<V1EnvVar> { new("LD_PRELOAD", existingPreload) }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            using (new AssertionScope())
            {
                container.Env.Should()
                    .Contain(x => x.Name == "CONTRAST_EXISTING_LD_PRELOAD" && x.Value == existingPreload);
                container.Env.Should().Contain(x =>
                    x.Name == "LD_PRELOAD" && x.Value ==
                    $"{context.ContrastMountPath}/runtimes/linux-x64/native/ContrastChainLoader.so:{existingPreload}");
            }
        }

        [Fact]
        public void PatchContainer_should_not_do_anything_if_chaining_disabled()
        {
            var patcher = new DotNetAgentPatcher();
            var context = AutoFixture.Create<PatchingContext>();
            var container = AutoFixture.Build<V1Container>().With(x => x.Env,
                new List<V1EnvVar> { new("CONTRAST__AGENT__DOTNET__ENABLE_CHAINING", "false") }).Create();

            // Act
            patcher.PatchContainer(container, context);

            // Assert
            using (new AssertionScope())
            {
                container.Env.Should().NotContain(x => x.Name == "CONTRAST_EXISTING_LD_PRELOAD");
                container.Env.Should().NotContain(x => x.Name == "LD_PRELOAD");
            }
        }
    }
}
