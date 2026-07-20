// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class ReconcileOnCreateTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "reconcile-oncreate";

    private readonly TestingContext _context;

    public ReconcileOnCreateTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_reconcile_oncreate_then_pod_is_injected()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        result.Spec.InitContainers.Should().ContainSingle(x => x.Name == "contrast-init")
              .Which.Image.Should().Be("contrast/agent-dummy:latest");
    }
}
