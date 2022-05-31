using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection
{
    public class MultipleImagesTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "multiple-images";

        private readonly TestingContext _context;

        public MultipleImagesTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_multiple_containers_exist_then_image_selector_should_be_used()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetInjectedPodByPrefix(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var nonInjectionContainer = result.Spec.Containers.Should().ContainSingle(x=>x.Name == "pause").Subject;
                nonInjectionContainer.Env.Should().BeNull();

                var injectionContainer = result.Spec.Containers.Should().ContainSingle(x=>x.Name == "busybox").Subject;
                injectionContainer.Env.Should().Contain(x => x.Name == "CONTRAST__API__URL");
            }
        }
    }
}
