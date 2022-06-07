using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection
{
    public class ClusterDefaultTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "injection-cluster";

        private readonly TestingContext _context;

        public ClusterDefaultTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_injection_annotations()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetInjectedPodByPrefix(ScenarioName);

            // Assert
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
        }

        [Fact]
        public async Task When_injected_then_pod_should_use_generated_secrets()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetInjectedPodByPrefix(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.Containers.Single();
                container.Env.Should().Contain(x => x.Name == "CONTRAST__API__URL").Which.Value.Should().Be("http://not-localhost");

                // Of course, this won't be here if telemetry is disabled.
                //container.Env.Should().Contain(x => x.Name == "CONTRAST_CLUSTER_ID").Which.Value.Should().NotBeNull();

                var apiKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__API_KEY").Subject;
                apiKey.ValueFrom.SecretKeyRef.Name.Should().Be("default-agent-connection-secret-cf80cd8aed");
                apiKey.ValueFrom.SecretKeyRef.Key.Should().Be("api-key");

                var serviceKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__SERVICE_KEY").Subject;
                serviceKey.ValueFrom.SecretKeyRef.Name.Should().Be("default-agent-connection-secret-cf80cd8aed");
                serviceKey.ValueFrom.SecretKeyRef.Key.Should().Be("service-key");

                var userName = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__USER_NAME").Subject;
                userName.ValueFrom.SecretKeyRef.Name.Should().Be("default-agent-connection-secret-cf80cd8aed");
                userName.ValueFrom.SecretKeyRef.Key.Should().Be("username");
            }
        }
    }
}
