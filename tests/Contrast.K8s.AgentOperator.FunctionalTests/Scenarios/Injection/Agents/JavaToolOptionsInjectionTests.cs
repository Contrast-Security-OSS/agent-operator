// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection.Agents;

public class JavaToolOptionsInjectionTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "injection-javatooloptions";

    private readonly TestingContext _context;

    public JavaToolOptionsInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_patched_java_tool_options_environment_variable()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.Containers.Should().ContainSingle().Subject;

            container.Env.Should().Contain(x => x.Name == "JAVA_TOOL_OPTIONS")
                .Which.Value.Should().Be("-javaagent:/opt/contrast/contrast-agent.jar something");
            container.Env.Should().Contain(x => x.Name == "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS")
                .Which.Value.Should().Be("something");
        }
    }
}
