// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection.Agents
{
    public class DotnetInjectionTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "injection-dotnet";

        private readonly TestingContext _context;

        public DotnetInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_environment_variables()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetInjectedPodByPrefix(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.Containers.Should().ContainSingle().Subject;

                container.Env.Should().Contain(x => x.Name == "CORECLR_PROFILER")
                         .Which.Value.Should().Be("{8B2CE134-0948-48CA-A4B2-80DDAD9F5791}");
                container.Env.Should().Contain(x => x.Name == "CORECLR_PROFILER_PATH")
                         .Which.Value.Should().Be("/contrast/runtimes/linux-x64/native/ContrastProfiler.so");
                container.Env.Should().Contain(x => x.Name == "CORECLR_ENABLE_PROFILING")
                         .Which.Value.Should().Be("1");
                container.Env.Should().Contain(x => x.Name == "CONTRAST_SOURCE")
                         .Which.Value.Should().Be("kubernetes-operator");
                container.Env.Should().Contain(x => x.Name == "CONTRAST_CORECLR_INSTALL_DIRECTORY")
                         .Which.Value.Should().Be("/contrast");
                container.Env.Should().Contain(x => x.Name == "CONTRAST__AGENT__DOTNET__ENABLE_FILE_WATCHING")
                         .Which.Value.Should().Be("false");
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_init_image()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetInjectedPodByPrefix(ScenarioName);

            // Assert
            result.Spec.InitContainers.Should().ContainSingle(x => x.Name == "contrast-init")
                  .Which.Image.Should().Be("contrast/agent-dotnet-core:latest");
        }
    }
}
