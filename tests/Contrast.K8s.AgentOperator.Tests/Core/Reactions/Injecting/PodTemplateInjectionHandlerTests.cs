// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;
using NSubstitute;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting
{
    public class PodTemplateInjectionHandlerTests
    {
        private static readonly Fixture AutoFixture = new();

        private const string InjName = "inj";
        private const string InjNamespace = "inj-ns";

        // Builds a Deployment target already carrying operator annotations, so that
        // ChangesNeeded is true against an empty desired state (injector == null in
        // the notification). This isolates the gate decision without a bundle.
        private static ResourceIdentityPair<IResourceWithPodTemplate> AnnotatedTarget()
        {
            var annotations = new List<MetadataAnnotations>
            {
                new(InjectionConstants.InjectorHashAttributeName, "old-hash"),
                new(InjectionConstants.InjectorNameAttributeName, InjName),
                new(InjectionConstants.InjectorNamespaceAttributeName, InjNamespace),
                new(InjectionConstants.WorkloadNameAttributeName, "workload"),
                new(InjectionConstants.WorkloadNamespaceAttributeName, "workload-ns"),
            };
            var deployment = AutoFixture.Create<DeploymentResource>() with
            {
                PodTemplate = AutoFixture.Create<PodTemplate>() with { Annotations = annotations }
            };
            var identity = NamespacedResourceIdentity.Create<DeploymentResource>("workload", "workload-ns");
            return new ResourceIdentityPair<IResourceWithPodTemplate>(identity, deployment);
        }

        // Same identity as AnnotatedTarget (workload/workload-ns) but the workload-name
        // annotation names a different workload — i.e. annotations copied from elsewhere.
        private static ResourceIdentityPair<IResourceWithPodTemplate> ForgedIdentityTarget()
        {
            var annotations = new List<MetadataAnnotations>
            {
                new(InjectionConstants.InjectorHashAttributeName, "old-hash"),
                new(InjectionConstants.InjectorNameAttributeName, InjName),
                new(InjectionConstants.InjectorNamespaceAttributeName, InjNamespace),
                new(InjectionConstants.WorkloadNameAttributeName, "some-other-workload"),
                new(InjectionConstants.WorkloadNamespaceAttributeName, "workload-ns"),
            };
            var deployment = AutoFixture.Create<DeploymentResource>() with
            {
                PodTemplate = AutoFixture.Create<PodTemplate>() with { Annotations = annotations }
            };
            var identity = NamespacedResourceIdentity.Create<DeploymentResource>("workload", "workload-ns");
            return new ResourceIdentityPair<IResourceWithPodTemplate>(identity, deployment);
        }

        private static (PodTemplateInjectionHandler handler, IStateContainer state, IResourcePatcher patcher, IResourceHasher hasher) CreateGraph()
        {
            var state = Substitute.For<IStateContainer>();
            state.GetIsDirty(Arg.Any<NamespacedResourceIdentity>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<bool>(false));
            var patcher = Substitute.For<IResourcePatcher>();
            var hasher = Substitute.For<IResourceHasher>();
            var handler = new PodTemplateInjectionHandler(hasher, state, patcher);
            return (handler, state, patcher, hasher);
        }

        // A Deployment with no operator annotations, used for the rule-1 opt-in patch.
        private static ResourceIdentityPair<IResourceWithPodTemplate> UnannotatedTarget()
        {
            var deployment = AutoFixture.Create<DeploymentResource>() with
            {
                PodTemplate = AutoFixture.Create<PodTemplate>() with { Annotations = new List<MetadataAnnotations>() }
            };
            var identity = NamespacedResourceIdentity.Create<DeploymentResource>("workload", "workload-ns");
            return new ResourceIdentityPair<IResourceWithPodTemplate>(identity, deployment);
        }

        // Single place that makes GetInjectorBundle (called inside GetDesiredState)
        // resolve to a non-null bundle and pins the computed hash. If GetInjectorBundle's
        // internals change, adjust here only. Returns the matched injector pair.
        private static ResourceIdentityPair<AgentInjectorResource> SetupResolvableBundle(
            IStateContainer state, IResourceHasher hasher, ReconcilePolicy policy, string hash)
        {
            var injectorResource = AutoFixture.Create<AgentInjectorResource>() with { ReconcilePolicy = policy };
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(injectorResource));
            state.GetById<AgentConnectionResource>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentConnectionResource?>(AutoFixture.Create<AgentConnectionResource>()));
            hasher.GetHash(Arg.Any<AgentInjectorResource>(), Arg.Any<AgentConnectionResource>(),
                    Arg.Any<AgentConfigurationResource?>(), Arg.Any<System.Collections.Generic.IEnumerable<SecretResource>>())
                .Returns(hash);

            var identity = NamespacedResourceIdentity.Create<AgentInjectorResource>(InjName, InjNamespace);
            return new ResourceIdentityPair<AgentInjectorResource>(identity, injectorResource);
        }

        [Fact]
        public async Task Handle_defers_patch_when_annotated_injector_is_OnCreate()
        {
            var (handler, state, patcher, _) = CreateGraph();
            var target = AnnotatedTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(
                    AutoFixture.Create<AgentInjectorResource>() with { ReconcilePolicy = ReconcilePolicy.OnCreate }));

            // injector == null: nothing matches now, desired state is empty, a patch to
            // strip annotations would otherwise occur.
            await handler.Handle(new InjectorMatched(target, null), CancellationToken.None);

            await patcher.DidNotReceive().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_patches_when_annotated_injector_is_Always()
        {
            var (handler, state, patcher, _) = CreateGraph();
            var target = AnnotatedTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(
                    AutoFixture.Create<AgentInjectorResource>() with { ReconcilePolicy = ReconcilePolicy.Always }));

            await handler.Handle(new InjectorMatched(target, null), CancellationToken.None);

            await patcher.Received().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_patches_when_annotated_injector_does_not_resolve()
        {
            var (handler, state, patcher, _) = CreateGraph();
            var target = AnnotatedTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>((AgentInjectorResource?)null));

            await handler.Handle(new InjectorMatched(target, null), CancellationToken.None);

            await patcher.Received().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_patches_unannotated_workload_even_under_OnCreate()
        {
            // Rule 1: a newly matched workload with no operator annotations gets the
            // opt-in first patch regardless of policy. The matched injector's bundle
            // resolves, so a real desired state differs from the empty annotations.
            var (handler, state, patcher, hasher) = CreateGraph();
            var target = UnannotatedTarget();
            var injectorPair = SetupResolvableBundle(state, hasher, ReconcilePolicy.OnCreate, "new-hash");

            await handler.Handle(new InjectorMatched(target, injectorPair), CancellationToken.None);

            await patcher.Received().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_defers_when_matched_injector_hash_drifted_under_OnCreate()
        {
            // Rule 2, production path: the injector still matches but a setting changed,
            // so the freshly computed hash ("new-hash") differs from the annotated
            // "old-hash". ChangesNeeded is true, and the annotated OnCreate injector
            // makes the gate defer.
            var (handler, state, patcher, hasher) = CreateGraph();
            var target = AnnotatedTarget();
            var injectorPair = SetupResolvableBundle(state, hasher, ReconcilePolicy.OnCreate, "new-hash");

            await handler.Handle(new InjectorMatched(target, injectorPair), CancellationToken.None);

            await patcher.DidNotReceive().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_patches_when_workload_identity_annotations_do_not_match_even_under_OnCreate()
        {
            var (handler, state, patcher, _) = CreateGraph();
            var target = ForgedIdentityTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(
                    AutoFixture.Create<AgentInjectorResource>() with { ReconcilePolicy = ReconcilePolicy.OnCreate }));

            await handler.Handle(new InjectorMatched(target, null), CancellationToken.None);

            await patcher.Received().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }
    }
}
