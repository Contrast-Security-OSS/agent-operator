using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection
{
    public class GlobSelectingTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "glob-selecting";

        private readonly TestingContext _context;

        public GlobSelectingTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_label_contains_glob_syntax_then_glob_should_be_used()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
        }

        [Fact]
        public async Task When_image_contains_glob_syntax_then_glob_should_be_used()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var nonInjectionContainer = result.Spec.Containers.Should().ContainSingle(x => x.Name == "ignore").Subject;
                nonInjectionContainer.Env.Should().BeNull();

                var injectionContainer = result.Spec.Containers.Should().ContainSingle(x => x.Name == "nginx").Subject;
                injectionContainer.Env.Should().Contain(x => x.Name == "CONTRAST__API__URL");
            }
        }
    }
}
