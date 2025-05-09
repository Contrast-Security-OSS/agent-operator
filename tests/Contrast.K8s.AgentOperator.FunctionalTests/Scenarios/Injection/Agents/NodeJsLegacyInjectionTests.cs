// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection.Agents;

public class NodeJsLegacyInjectionTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "injection-nodejs-require";

    private readonly TestingContext _context;

    public NodeJsLegacyInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
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
                     .Which.Value.Should().Be("--require /contrast/agent/node_modules/@contrast/agent");
            container.Env.Should().Contain(x => x.Name == "CONTRAST__AGENT__LOGGER__PATH")
                     .Which.Value.Should().Be("/contrast/data/logs/contrast_agent.log");
            container.Env.Should().Contain(x => x.Name == "CONTRAST_INSTALLATION_TOOL")
                     .Which.Value.Should().Be("KUBERNETES_OPERATOR");
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
