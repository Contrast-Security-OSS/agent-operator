using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection
{
    public class TypesTests : IClassFixture<TestingContext>
    {
        private readonly TestingContext _context;

        public TypesTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_injection_target_is_a_demon_set_then_annotations_should_be_injected()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>("type-daemonset");

            // Assert
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
        }

        [Fact]
        public async Task When_injection_target_is_a_stateful_set_then_annotations_should_be_injected()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>("type-statefulset");

            // Assert
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
        }

        [Fact]
        public async Task When_injection_target_is_a_deployment_set_then_annotations_should_be_injected()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>("type-deployment");

            // Assert
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
        }
    }
}
