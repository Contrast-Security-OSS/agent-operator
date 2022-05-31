using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection.Agents
{
    public class NodeJsInjectionTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "injection-nodejs";

        private readonly TestingContext _context;

        public NodeJsInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
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
                container.Env.Should().Contain(x => x.Name == "NODE_OPTIONS")
                         .Which.Value.Should().Be("--require /contrast/node_modules/@contrast/agent");
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
                  .Which.Image.Should().Be("contrast/agent-nodejs:latest");
        }
    }
}
