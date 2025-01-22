// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class OldAuthTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "oldauth-dummy";

    private readonly TestingContext _context;

    public OldAuthTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_config_variables()
    {
        var client = await _context.GetClient(defaultNamespace: "testing-token");

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.Containers.Should().ContainSingle().Subject;

            var apiKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__API_KEY").Subject;
            apiKey.ValueFrom.SecretKeyRef.Name.Should().Be("oldauth-agent-connection-secret");
            apiKey.ValueFrom.SecretKeyRef.Key.Should().Be("apiKey");

            var serviceKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__SERVICE_KEY").Subject;
            serviceKey.ValueFrom.SecretKeyRef.Name.Should().Be("oldauth-agent-connection-secret");
            serviceKey.ValueFrom.SecretKeyRef.Key.Should().Be("serviceKey");

            var userName = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__USER_NAME").Subject;
            userName.ValueFrom.SecretKeyRef.Name.Should().Be("oldauth-agent-connection-secret");
            userName.ValueFrom.SecretKeyRef.Key.Should().Be("userName");
        }

    }
}
