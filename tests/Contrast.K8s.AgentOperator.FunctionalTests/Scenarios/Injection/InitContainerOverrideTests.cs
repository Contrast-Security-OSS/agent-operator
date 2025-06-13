// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class InitContainerOverrideTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "init-container-overrides";

    private readonly TestingContext _context;

    public InitContainerOverrideTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_any_overrides_are_applied_a_debugging_env_var_should_be_added()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.InitContainers.Single(x => x.Name == "contrast-init");
            var env = container.Env;

            env.Should().ContainSingle(x => x.Name == "CONTRAST_DEBUGGING_SECURITY_CONTEXT_TAINTED").Which.Value.Should().Be("True");
        }
    }

    [Fact]
    public async Task When_init_container_overrides_exist_then_the_overrides_should_be_merged_and_applied()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.InitContainers.Single(x => x.Name == "contrast-init");
            var context = container.SecurityContext;

            context.Should().NotBeNull();
            context.RunAsUser.Should().Be(499);
            context.RunAsNonRoot.Should().BeFalse();
        }
    }
}
