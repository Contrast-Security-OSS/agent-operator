// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using NSubstitute;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting.Patching;

public class PodPatcherImageVolumeTests
{
    private static PodPatcher CreatePatcher(
        OperatorOptions? operatorOptions = null,
        InitContainerOptions? initOptions = null,
        TelemetryOptions? telemetryOptions = null,
        IAgentPatcher? agentPatcher = null)
    {
        operatorOptions ??= new OperatorOptions(
            Namespace: "default",
            SettlingDurationSeconds: 5,
            WatcherTimeoutSeconds: 30,
            EventQueueSize: 100,
            EventQueueFullMode: System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            EventQueueMergeWindowSeconds: 1,
            RunInitContainersAsNonRoot: false,
            SuppressSeccompProfile: false,
            EnableAgentStdout: false,
            ChaosRatio: 0,
            UseImageVolumes: false
        );

        initOptions ??= new InitContainerOptions("100m", "500m", "64Mi", "256Mi", "128Mi", "512Mi");
        telemetryOptions ??= new TelemetryOptions(false, "cluster-id", "default", "operator");

        var patchers = agentPatcher != null
            ? new[] { agentPatcher }
            : Array.Empty<IAgentPatcher>();

        var globMatcher = Substitute.For<IGlobMatcher>();
        globMatcher.Matches(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var clusterIdState = Substitute.For<IClusterIdState>();

        return new PodPatcher(
            () => patchers,
            globMatcher,
            clusterIdState,
            operatorOptions,
            initOptions,
            telemetryOptions
        );
    }

    private static PatchingContext CreateContext(AgentInjectionType type = AgentInjectionType.Java)
    {
        var image = new ContainerImageReference("docker.io", "contrast/agent-java", "latest");
        var selector = new ResourceWithPodSpecSelector(
            new List<string> { "*" },
            new List<LabelPattern>(),
            new List<string> { "default" }
        );
        var connectionRef = new AgentConnectionReference("default", "my-connection");
        var configRef = new AgentConfigurationReference("default", "my-config");

        var injector = new AgentInjectorResource(
            Enabled: true,
            Type: type,
            Image: image,
            Selector: selector,
            ConnectionReference: connectionRef,
            ConfigurationReference: configRef,
            ImagePullSecret: null,
            ImagePullPolicy: "IfNotPresent"
        );

        var connection = new AgentConnectionResource(
            MountAsVolume: false,
            Token: null,
            TeamServerUri: "https://app.contrastsecurity.com",
            ApiKey: new SecretReference("default", "contrast-secret", "apiKey"),
            ServiceKey: new SecretReference("default", "contrast-secret", "serviceKey"),
            UserName: new SecretReference("default", "contrast-secret", "userName")
        );

        return new PatchingContext(
            WorkloadName: "my-app",
            WorkloadNamespace: "default",
            Injector: injector,
            Connection: connection,
            Configuration: null,
            ConnectionVolumeSecret: null,
            AgentMountPath: "/contrast/agent",
            WritableMountPath: "/contrast/data"
        );
    }

    private static V1Pod CreatePod()
    {
        return new V1Pod
        {
            Metadata = new V1ObjectMeta
            {
                Name = "my-app-pod",
                NamespaceProperty = "default"
            },
            Spec = new V1PodSpec
            {
                Containers = new List<V1Container>
                {
                    new()
                    {
                        Name = "app",
                        Image = "myapp:latest"
                    }
                }
            }
        };
    }

    // --- Default (non-image-volume) behavior ---

    [Fact]
    public async Task When_image_volumes_disabled_then_init_container_should_be_present()
    {
        var patcher = CreatePatcher();
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Spec.InitContainers.Should().ContainSingle(c => c.Name == "contrast-init");
    }

    [Fact]
    public async Task When_image_volumes_disabled_then_agent_volume_should_use_emptydir()
    {
        var patcher = CreatePatcher();
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var agentVolume = pod.Spec.Volumes.Single(v => v.Name == "contrast-agent");
        using (new AssertionScope())
        {
            agentVolume.EmptyDir.Should().NotBeNull();
            agentVolume.Image.Should().BeNull();
        }
    }

    [Fact]
    public async Task When_image_volumes_disabled_then_injection_mode_annotation_should_not_be_set()
    {
        var patcher = CreatePatcher();
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Metadata.Annotations.Should().NotContainKey(InjectionConstants.InjectionModeAttributeName);
    }

    // --- Image volume behavior ---

    [Fact]
    public async Task When_image_volumes_enabled_then_init_container_should_not_be_present()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);
        var patcher = CreatePatcher(operatorOptions: options);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Spec.InitContainers.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_agent_volume_should_use_image_source()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);
        var patcher = CreatePatcher(operatorOptions: options);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var agentVolume = pod.Spec.Volumes.Single(v => v.Name == "contrast-agent");
        using (new AssertionScope())
        {
            agentVolume.Image.Should().NotBeNull();
            agentVolume.Image.Reference.Should().Be("docker.io/contrast/agent-java:latest");
            agentVolume.Image.PullPolicy.Should().Be("IfNotPresent");
            agentVolume.EmptyDir.Should().BeNull();
        }
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_injection_mode_annotation_should_be_set()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);
        var patcher = CreatePatcher(operatorOptions: options);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Metadata.Annotations.Should()
            .ContainKey(InjectionConstants.InjectionModeAttributeName)
            .WhoseValue.Should().Be("image-volume");
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_agent_volume_mount_should_use_image_volume_path()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);
        var patcher = CreatePatcher(operatorOptions: options);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var agentMount = container.VolumeMounts.Single(vm => vm.Name == "contrast-agent");
        using (new AssertionScope())
        {
            agentMount.MountPath.Should().Be("/contrast/agent");
            agentMount.ReadOnlyProperty.Should().BeTrue();
        }
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_writable_volume_should_still_be_emptydir()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);
        var patcher = CreatePatcher(operatorOptions: options);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var writableVolume = pod.Spec.Volumes.Single(v => v.Name == "contrast-writable");
        writableVolume.EmptyDir.Should().NotBeNull();
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_env_vars_should_use_image_volume_mount_path()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);
        var patcher = CreatePatcher(operatorOptions: options);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var mountPathEnv = container.Env.Single(e => e.Name == "CONTRAST_MOUNT_PATH");
        var agentPathEnv = container.Env.Single(e => e.Name == "CONTRAST_MOUNT_AGENT_PATH");
        using (new AssertionScope())
        {
            // AgentMountPath is set to imageVolumeMountPath + "/contrast" = "/contrast/agent/contrast"
            mountPathEnv.Value.Should().Be("/contrast/agent/contrast");
            agentPathEnv.Value.Should().Be("/contrast/agent/contrast");
        }
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_agent_patcher_mount_path_override_should_be_skipped()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);

        var mockPatcher = Substitute.For<IAgentPatcher>();
        mockPatcher.Type.Returns(AgentInjectionType.Java);
        mockPatcher.GetOverrideAgentMountPath().Returns("/opt/contrast");
        mockPatcher.GenerateEnvVars(Arg.Any<PatchingContext>()).Returns(callInfo =>
        {
            var ctx = callInfo.Arg<PatchingContext>();
            return new[]
            {
                new V1EnvVar { Name = "TEST_AGENT_MOUNT_PATH", Value = ctx.AgentMountPath }
            };
        });

        var patcher = CreatePatcher(operatorOptions: options, agentPatcher: mockPatcher);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var testEnv = container.Env.Single(e => e.Name == "TEST_AGENT_MOUNT_PATH");
        // Should use image volume path, not the Java override of /opt/contrast
        testEnv.Value.Should().Be("/contrast/agent/contrast");
    }

    [Fact]
    public async Task When_image_volumes_disabled_then_agent_patcher_mount_path_override_should_apply()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: false);

        var mockPatcher = Substitute.For<IAgentPatcher>();
        mockPatcher.Type.Returns(AgentInjectionType.Java);
        mockPatcher.GetOverrideAgentMountPath().Returns("/opt/contrast");
        mockPatcher.GenerateEnvVars(Arg.Any<PatchingContext>()).Returns(callInfo =>
        {
            var ctx = callInfo.Arg<PatchingContext>();
            return new[]
            {
                new V1EnvVar { Name = "TEST_AGENT_MOUNT_PATH", Value = ctx.AgentMountPath }
            };
        });

        var patcher = CreatePatcher(operatorOptions: options, agentPatcher: mockPatcher);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var testEnv = container.Env.Single(e => e.Name == "TEST_AGENT_MOUNT_PATH");
        // Should use the Java override path
        testEnv.Value.Should().Be("/opt/contrast");
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_standard_annotations_should_still_be_set()
    {
        var options = new OperatorOptions("default", 5, 30, 100,
            System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            1, false, false, false, 0, UseImageVolumes: true);
        var patcher = CreatePatcher(operatorOptions: options);
        var context = CreateContext();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        using (new AssertionScope())
        {
            pod.Metadata.Annotations.Should().ContainKey(InjectionConstants.IsInjectedAttributeName)
                .WhoseValue.Should().Be("True");
            pod.Metadata.Annotations.Should().ContainKey(InjectionConstants.InjectedOnAttributeName);
            pod.Metadata.Annotations.Should().ContainKey(InjectionConstants.InjectedByAttributeName);
            pod.Metadata.Annotations.Should().ContainKey(InjectionConstants.InjectorTypeAttributeName)
                .WhoseValue.Should().Be("Java");
        }
    }
}
