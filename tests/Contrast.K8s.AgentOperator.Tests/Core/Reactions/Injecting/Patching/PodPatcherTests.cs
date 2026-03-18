// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using AutoFixture;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting.Patching;

public class PodPatcherTests
{
    private static readonly Fixture AutoFixture = new();

    private static PodPatcher CreatePatcher(
        OperatorOptions? operatorOptions = null,
        InitContainerOptions? initOptions = null,
        TelemetryOptions? telemetryOptions = null,
        IAgentPatcher? agentPatcher = null)
    {
        operatorOptions ??= AutoFixture.Create<OperatorOptions>();

        initOptions ??= new InitContainerOptions("100m", "500m", "64Mi", "256Mi", "128Mi", "512Mi");
        telemetryOptions ??= AutoFixture.Create<TelemetryOptions>();

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

    // -- init-containers --

    [Fact]
    public async Task When_image_volumes_disabled_then_init_container_should_be_present()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, false).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Spec.InitContainers.Should().ContainSingle(c => c.Name == "contrast-init");
    }

    [Fact]
    public async Task When_image_volumes_disabled_then_agent_volume_should_use_emptydir()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, false).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
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
    public async Task When_image_volumes_disabled_injection_mode_annotation_should_be_set_to_init_container()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, false).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Metadata.Annotations.Should().Contain(InjectionConstants.InjectionModeAttributeName, "init-container");
    }

    //-- image-volume --

    [Fact]
    public async Task When_image_volumes_enabled_then_init_container_should_not_be_present()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, true).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Spec.InitContainers.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_agent_volume_should_use_image_source()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, true).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var agentVolume = pod.Spec.Volumes.Single(v => v.Name == "contrast-agent");
        using (new AssertionScope())
        {
            agentVolume.Image.Should().NotBeNull();
            agentVolume.Image.Reference.Should().Be(context.Injector.Image.GetFullyQualifiedContainerImageName());
            agentVolume.Image.PullPolicy.Should().Be(context.Injector.ImagePullPolicy);
            agentVolume.EmptyDir.Should().BeNull();
        }
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_injection_mode_annotation_should_be_set()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, true).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        pod.Metadata.Annotations.Should()
            .ContainKey(InjectionConstants.InjectionModeAttributeName)
            .WhoseValue.Should().Be("image-volume");
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_agent_volume_mount_should_use_image_volume_path()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, true).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var agentMount = container.VolumeMounts.Single(vm => vm.Name == "contrast-agent");
        using (new AssertionScope())
        {
            agentMount.MountPath.Should().Be(context.AgentMountPath);
            agentMount.ReadOnlyProperty.Should().BeTrue();
        }
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_writable_volume_should_still_be_emptydir()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, true).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var writableVolume = pod.Spec.Volumes.Single(v => v.Name == "contrast-writable");
        writableVolume.EmptyDir.Should().NotBeNull();
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_env_vars_should_use_image_volume_mount_path()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, true).Create();
        var patcher = CreatePatcher(operatorOptions: options);
        var context = AutoFixture.Create<PatchingContext>();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var mountPathEnv = container.Env.Single(e => e.Name == "CONTRAST_MOUNT_PATH");
        var agentPathEnv = container.Env.Single(e => e.Name == "CONTRAST_MOUNT_AGENT_PATH");
        using (new AssertionScope())
        {
            // AgentMountPath is set to AgentMountPath + "/contrast" = "/contrast/agent/contrast"
            mountPathEnv.Value.Should().Be(context.AgentMountPath + "/contrast");
            agentPathEnv.Value.Should().Be(context.AgentMountPath + "/contrast");
        }
    }

    [Fact]
    public async Task When_image_volumes_enabled_then_agent_patcher_mount_path_override_should_be_skipped()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, true).Create();
        var overridePath = AutoFixture.Create<string>();
        var mockPatcher = Substitute.For<IAgentPatcher>();
        mockPatcher.Type.Returns(AgentInjectionType.Dummy);
        mockPatcher.GetOverrideAgentMountPath().Returns(overridePath);
        mockPatcher.GenerateEnvVars(Arg.Any<PatchingContext>()).Returns(callInfo =>
        {
            var ctx = callInfo.Arg<PatchingContext>();
            return new[]
            {
                new V1EnvVar { Name = "TEST_AGENT_MOUNT_PATH", Value = ctx.AgentMountPath }
            };
        });

        var patcher = CreatePatcher(operatorOptions: options, agentPatcher: mockPatcher);
        var injector = AutoFixture.Build<AgentInjectorResource>().With(x => x.Type, AgentInjectionType.Dummy).Create();
        var context = AutoFixture.Build<PatchingContext>()
            .With(x=> x.Injector, injector)
            .With(x => x.AgentMountPath, "/contrast/agent")
            .Create();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var testEnv = container.Env.Single(e => e.Name == "TEST_AGENT_MOUNT_PATH");
        // Should use image volume path, not override
        testEnv.Value.Should().Be(context.AgentMountPath + "/contrast");
    }

    [Fact]
    public async Task When_image_volumes_disabled_then_agent_patcher_mount_path_override_should_apply()
    {
        var options = AutoFixture.Build<OperatorOptions>().With(x => x.UseImageVolumes, false).Create();
        var overridePath = AutoFixture.Create<string>();
        var mockPatcher = Substitute.For<IAgentPatcher>();
        mockPatcher.Type.Returns(AgentInjectionType.Dummy);
        mockPatcher.GetOverrideAgentMountPath().Returns(overridePath);
        mockPatcher.GenerateEnvVars(Arg.Any<PatchingContext>()).Returns(callInfo =>
        {
            var ctx = callInfo.Arg<PatchingContext>();
            return new[]
            {
                new V1EnvVar { Name = "TEST_AGENT_MOUNT_PATH", Value = ctx.AgentMountPath }
            };
        });

        var patcher = CreatePatcher(operatorOptions: options, agentPatcher: mockPatcher);
        var injector = AutoFixture.Build<AgentInjectorResource>().With(x => x.Type, AgentInjectionType.Dummy).Create();
        var context = AutoFixture.Build<PatchingContext>()
            .With(x => x.Injector, injector)
            .Create();
        var pod = CreatePod();

        await patcher.Patch(context, pod);

        var container = pod.Spec.Containers.First();
        var testEnv = container.Env.Single(e => e.Name == "TEST_AGENT_MOUNT_PATH");
        testEnv.Value.Should().Be(overridePath);
    }
}
