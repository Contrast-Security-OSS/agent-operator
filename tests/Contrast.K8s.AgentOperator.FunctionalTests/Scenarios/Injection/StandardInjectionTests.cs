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

public class StandardInjectionTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "injection-dummy";

    private readonly TestingContext _context;

    public StandardInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
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
        using (new AssertionScope())
        {
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injected-on");
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injector-hash");
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injector-name").WhoseValue.Should().Be(ScenarioName);
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injector-namespace").WhoseValue.Should().Be("testing");
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
            result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injector-type").WhoseValue.Should().Be("Dummy");
        }
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_standard_injection_environment_variables()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.Containers.Single();
            container.Env.Should().Contain(x => x.Name == "CONTRAST_MOUNT_PATH").Which.Value.Should().Be("/contrast/agent");
            container.Env.Should().Contain(x => x.Name == "CONTRAST_MOUNT_AGENT_PATH").Which.Value.Should().Be("/contrast/agent");
            container.Env.Should().Contain(x => x.Name == "CONTRAST_MOUNT_WRITABLE_PATH").Which.Value.Should().Be("/contrast/data");
            container.Env.Should().Contain(x => x.Name == "CONTRAST__API__URL").Which.Value.Should().Be("http://localhost");

            // Of course, this won't be here if telemetry is disabled.
            //container.Env.Should().Contain(x => x.Name == "CONTRAST_CLUSTER_ID").Which.Value.Should().NotBeNull();

            var token = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__TOKEN").Subject;
            token.ValueFrom.SecretKeyRef.Name.Should().Be("testing-agent-connection-secret");
            token.ValueFrom.SecretKeyRef.Key.Should().Be("token");

            var apiKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__API_KEY").Subject;
            apiKey.ValueFrom.SecretKeyRef.Name.Should().Be("testing-agent-connection-secret");
            apiKey.ValueFrom.SecretKeyRef.Key.Should().Be("apiKey");

            var serviceKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__SERVICE_KEY").Subject;
            serviceKey.ValueFrom.SecretKeyRef.Name.Should().Be("testing-agent-connection-secret");
            serviceKey.ValueFrom.SecretKeyRef.Key.Should().Be("serviceKey");

            var userName = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__USER_NAME").Subject;
            userName.ValueFrom.SecretKeyRef.Name.Should().Be("testing-agent-connection-secret");
            userName.ValueFrom.SecretKeyRef.Key.Should().Be("userName");

            container.Env.Should().Contain(x => x.Name == "CONTRAST__SERVER__NAME")
                     .Which.Value.Should().Be("kubernetes-testing");
            container.Env.Should().Contain(x => x.Name == "CONTRAST__APPLICATION__NAME")
                     .Which.Value.Should().Be("injection-dummy");
        }
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_volume_mounts()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.Containers.Single();

            var agentMount = container.VolumeMounts.Should().ContainSingle(x => x.Name == "contrast-agent").Subject;
            agentMount.MountPath.Should().Be("/contrast/agent");
            agentMount.ReadOnlyProperty.Should().BeTrue();

            var writableMount = container.VolumeMounts.Should().ContainSingle(x => x.Name == "contrast-writable").Subject;
            writableMount.MountPath.Should().Be("/contrast/data");
            writableMount.ReadOnlyProperty.Should().NotBeTrue();
        }
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_injection_common_config_environment_variables()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.Containers.Single();
            container.Env.Should().Contain(x => x.Name == "CONTRAST__ENABLED").Which.Value.Should().Be("false");
            container.Env.Should().Contain(x => x.Name == "CONTRAST__FOO__BAR").Which.Value.Should().Be("foobar");
        }
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_injection_init_container()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.InitContainers.Single(x => x.Name == "contrast-init");
            container.ImagePullPolicy.Should().Be("Always");

            container.VolumeMounts.Should().Contain(x => x.Name == "contrast-agent").Which.MountPath.Should().Be("/contrast-init/agent");
            container.VolumeMounts.Should().Contain(x => x.Name == "contrast-writable").Which.MountPath.Should().Be("/contrast-init/data");

            container.Env.Should().Contain(x => x.Name == "CONTRAST_MOUNT_PATH").Which.Value.Should().Be("/contrast-init/agent");
            container.Env.Should().Contain(x => x.Name == "CONTRAST_MOUNT_AGENT_PATH").Which.Value.Should().Be("/contrast-init/agent");
            container.Env.Should().Contain(x => x.Name == "CONTRAST_MOUNT_WRITABLE_PATH").Which.Value.Should().Be("/contrast-init/data");
        }
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_injection_volume()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            result.Spec.Volumes.Should().ContainSingle(x => x.Name == "contrast-agent")
                  .Which.EmptyDir.Should().NotBeNull();

            result.Spec.Volumes.Should().ContainSingle(x => x.Name == "contrast-writable")
                  .Which.EmptyDir.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task When_init_container_is_created_then_default_security_context_should_be_applied()
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
            context.Capabilities.Add.Should().BeNullOrEmpty();
            context.Capabilities.Drop.Should().ContainSingle().Which.Should().Be("ALL");
            context.Privileged.Should().BeFalse();
            context.ReadOnlyRootFilesystem.Should().BeTrue();
            context.AllowPrivilegeEscalation.Should().BeFalse();
            context.SeccompProfile?.Type.Should().Be("RuntimeDefault");

            // These should always be null for OpenShift.
            context.RunAsUser.Should().BeNull();
            context.RunAsGroup.Should().BeNull();

            // This should be true in our tests, since we CONTRAST_RUN_INIT_CONTAINER_AS_NON_ROOT.
            context.RunAsNonRoot.Should().BeTrue();
        }
    }

    [Fact]
    public async Task When_init_container_is_created_then_default_resource_limits_should_be_applied()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.InitContainers.Single(x => x.Name == "contrast-init");
            var resources = container.Resources;

            resources.Should().NotBeNull();
            resources.Limits.Should().ContainKey("cpu").WhoseValue.Value.Should().Be("100m");
            resources.Limits.Should().ContainKey("memory").WhoseValue.Value.Should().Be("256Mi");
            resources.Requests.Should().ContainKey("cpu").WhoseValue.Value.Should().Be("100m");
            resources.Requests.Should().ContainKey("memory").WhoseValue.Value.Should().Be("64Mi");
        }
    }
}
