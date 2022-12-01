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
    public class JavaInjectionTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "injection-java";

        private readonly TestingContext _context;

        public JavaInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
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

                container.Env.Should().Contain(x => x.Name == "JAVA_TOOL_OPTIONS")
                         .Which.Value.Should().Be("-javaagent:/opt/contrast/contrast-agent.jar");
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_volume_mount()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetInjectedPodByPrefix(ScenarioName);

            // Assert
            result.Spec.Containers.Should().ContainSingle()
                  .Which.VolumeMounts.Should().ContainSingle(x => x.Name == "contrast-agent")
                  .Which.MountPath.Should().Be("/opt/contrast");
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_init_image()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetInjectedPodByPrefix(ScenarioName);

            // Assert
            result.Spec.InitContainers.Should().ContainSingle(x => x.Name == "contrast-init")
                  .Which.Image.Should().Be("contrast/agent-java:latest");
        }
    }
}
