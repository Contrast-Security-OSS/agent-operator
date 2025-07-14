// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class CustomVersionTest : IClassFixture<TestingContext>
{
    private const string ScenarioName = "custom-version";

    private readonly TestingContext _context;

    public CustomVersionTest(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_version_is_configured_then_init_container_should_use_version()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        result.Spec.InitContainers.Should().ContainSingle(x => x.Name == "contrast-init")
              .Which.Image.Should().Be("contrast/agent-dummy:1.0");
    }
}
