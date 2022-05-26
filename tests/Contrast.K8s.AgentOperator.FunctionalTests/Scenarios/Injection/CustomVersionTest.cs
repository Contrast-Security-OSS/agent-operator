using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection
{
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
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            result.Spec.InitContainers.Should().ContainSingle(x => x.Name == "contrast-init")
                  .Which.Image.Should().Be("docker.io/library/busybox:1.0");
        }
    }
}
