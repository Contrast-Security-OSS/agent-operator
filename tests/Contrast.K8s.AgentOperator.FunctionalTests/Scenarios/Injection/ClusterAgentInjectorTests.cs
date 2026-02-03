// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class ClusterAgentInjectorTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "injection-cluster-injector";

    private readonly TestingContext _context;

    public ClusterAgentInjectorTests(TestingContext context, ITestOutputHelper outputHelper)
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
            container.Env.Should().Contain(x => x.Name == "CONTRAST__API__URL").Which.Value.Should().Be("http://localhost_cluster_injector");

            // Of course, this won't be here if telemetry is disabled.
            //container.Env.Should().Contain(x => x.Name == "CONTRAST_CLUSTER_ID").Which.Value.Should().NotBeNull();

            var token = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__TOKEN").Subject;
            token.ValueFrom.SecretKeyRef.Name.Should().Be("default-agent-injector-connectionsecret-dummy-cf80cd8aed");
            token.ValueFrom.SecretKeyRef.Key.Should().Be("token");

            var apiKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__API_KEY").Subject;
            apiKey.ValueFrom.SecretKeyRef.Name.Should().Be("default-agent-injector-connectionsecret-dummy-cf80cd8aed");
            apiKey.ValueFrom.SecretKeyRef.Key.Should().Be("api-key");

            var serviceKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__SERVICE_KEY").Subject;
            serviceKey.ValueFrom.SecretKeyRef.Name.Should().Be("default-agent-injector-connectionsecret-dummy-cf80cd8aed");
            serviceKey.ValueFrom.SecretKeyRef.Key.Should().Be("service-key");

            var userName = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__USER_NAME").Subject;
            userName.ValueFrom.SecretKeyRef.Name.Should().Be("default-agent-injector-connectionsecret-dummy-cf80cd8aed");
            userName.ValueFrom.SecretKeyRef.Key.Should().Be("username");
        }
    }

    [Fact]
    public async Task When_injected_then_pod_should_use_generated_configuration()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.Containers.Single();
            container.Env.Should().Contain(x => x.Name == "CONTRAST__FOO__BAR").Which.Value.Should().Be("foobar_cluster_injector");
        }
    }

    [Fact]
    public async Task Pod_should_have_image_pullsecret()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        result.Spec.ImagePullSecrets.Should().ContainSingle(x => x.Name.StartsWith("default-agent-injector-pullsecret"));
    }

}
