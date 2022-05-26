using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection.Agents
{
    public class DotnetChainingInjectionTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "injection-dotnet-chaining";

        private readonly TestingContext _context;

        public DotnetChainingInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_chaining_environment_variables()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.Containers.Should().ContainSingle().Subject;

                container.Env.Should().Contain(x => x.Name == "LD_PRELOAD")
                         .Which.Value.Should().Be("/contrast/runtimes/linux-x64/native/ContrastChainLoader.so:something");
                container.Env.Should().Contain(x => x.Name == "CONTRAST_EXISTING_LD_PRELOAD")
                         .Which.Value.Should().Be("something");
            }
        }
    }
}
