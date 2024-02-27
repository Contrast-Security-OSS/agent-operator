// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class MissingDependenciesTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "missing-deps";

    private readonly TestingContext _context;

    public MissingDependenciesTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_injection_configuration_is_missing_required_dependencies_then_pod_should_not_be_modified()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetByPrefix<V1Pod>(ScenarioName);

        // Assert
        result.Annotations().Should().BeNull();
    }
}
