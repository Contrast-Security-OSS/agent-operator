// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class TokenTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "token-dummy";

    private readonly TestingContext _context;

    public TokenTests(TestingContext context, ITestOutputHelper outputHelper)
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

            var token = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__TOKEN").Subject;
            token.ValueFrom.SecretKeyRef.Name.Should().Be("token-agent-connection-secret");
            token.ValueFrom.SecretKeyRef.Key.Should().Be("token");
        }

    }
}
