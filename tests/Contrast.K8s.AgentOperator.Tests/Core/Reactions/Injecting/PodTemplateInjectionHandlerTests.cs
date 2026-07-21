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
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
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
        private const string MatchKey = "contrast-agent";
        private const string MatchValue = "java";

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
                Labels = new List<MetadataLabel> { new(MatchKey, MatchValue) },
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

        // Same annotations as AnnotatedTarget (for THIS workload), but the deployment
        // carries no matching selector label — e.g. the label was removed.
        private static ResourceIdentityPair<IResourceWithPodTemplate> AnnotatedTargetWithoutMatchLabel()
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
                Labels = new List<MetadataLabel>(),   // no matching label
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
            var matcher = new AgentInjectorMatcher(new GlobMatcher());
            var handler = new PodTemplateInjectionHandler(hasher, state, patcher, matcher);
            return (handler, state, patcher, hasher);
        }

        // Builds an enabled OnCreate injector whose selector matches AnnotatedTarget
        // (namespace "workload-ns", label MatchKey=MatchValue).
        private static AgentInjectorResource EnabledOnCreateInjectorSelectingTarget()
        {
            return AutoFixture.Create<AgentInjectorResource>() with
            {
                Enabled = true,
                ReconcilePolicy = ReconcilePolicy.OnCreate,
                Selector = new ResourceWithPodSpecSelector(
                    new[] { "*" },
                    new[] { new LabelPattern(MatchKey, MatchValue) },
                    new[] { "workload-ns" })
            };
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
        // injectorName/injectorNamespace default to the standard InjName/InjNamespace
        // identity but can be overridden to stand up a SECOND, distinct resolvable
        // injector (e.g. to exercise a rebind onto a different injector than the one
        // named in a workload's stale annotations).
        private static ResourceIdentityPair<AgentInjectorResource> SetupResolvableBundle(
            IStateContainer state, IResourceHasher hasher, ReconcilePolicy policy, string hash,
            string injectorName = InjName, string injectorNamespace = InjNamespace)
        {
            var injectorResource = AutoFixture.Create<AgentInjectorResource>() with
            {
                ReconcilePolicy = policy,
                Enabled = true,
                Selector = new ResourceWithPodSpecSelector(
                    new[] { "*" },
                    new[] { new LabelPattern(MatchKey, MatchValue) },
                    new[] { "workload-ns" })
            };
            state.GetById<AgentInjectorResource>(injectorName, injectorNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(injectorResource));
            state.GetById<AgentConnectionResource>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentConnectionResource?>(AutoFixture.Create<AgentConnectionResource>()));
            hasher.GetHash(Arg.Any<AgentInjectorResource>(), Arg.Any<AgentConnectionResource>(),
                    Arg.Any<AgentConfigurationResource?>(), Arg.Any<System.Collections.Generic.IEnumerable<SecretResource>>())
                .Returns(hash);

            var identity = NamespacedResourceIdentity.Create<AgentInjectorResource>(injectorName, injectorNamespace);
            return new ResourceIdentityPair<AgentInjectorResource>(identity, injectorResource);
        }

        [Fact]
        public async Task Handle_defers_patch_when_annotated_injector_is_OnCreate()
        {
            var (handler, state, patcher, _) = CreateGraph();
            var target = AnnotatedTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(EnabledOnCreateInjectorSelectingTarget()));

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

        [Fact]
        public async Task Handle_patches_when_workload_no_longer_matches_selector_under_OnCreate()
        {
            // Label removed: annotated injector is enabled + OnCreate, but its selector no
            // longer matches this workload, so the binding is stale and must be stripped.
            var (handler, state, patcher, _) = CreateGraph();
            var target = AnnotatedTargetWithoutMatchLabel();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(EnabledOnCreateInjectorSelectingTarget()));

            await handler.Handle(new InjectorMatched(target, null), CancellationToken.None);

            await patcher.Received().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_patches_when_annotated_injector_disabled_under_OnCreate()
        {
            // Disabled injector: binding is no longer active, strip.
            var (handler, state, patcher, _) = CreateGraph();
            var target = AnnotatedTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(
                    EnabledOnCreateInjectorSelectingTarget() with { Enabled = false }));

            await handler.Handle(new InjectorMatched(target, null), CancellationToken.None);

            await patcher.Received().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_patches_when_annotated_binding_invalid_but_different_injector_matches_under_OnCreate()
        {
            // Rebind / non-null fall-through: every other new test in this file passes
            // injector == null on the notification. Here ShouldDeferForOnCreate returns
            // false because the ANNOTATED injector A (InjName/InjNamespace) is disabled,
            // so its OnCreate binding is no longer active. But the InjectorMatched
            // notification carries a DIFFERENT, resolvable, enabled injector B (the
            // matcher found a fresh, live match elsewhere). GetDesiredState resolves
            // against the notification's injector B (not the stale annotated A),
            // producing a new hash that differs from the annotated "old-hash" and
            // triggering a re-patch that rebinds the workload onto B.
            var (handler, state, patcher, hasher) = CreateGraph();
            var target = AnnotatedTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(EnabledOnCreateInjectorSelectingTarget() with { Enabled = false }));

            const string otherInjectorName = "inj-b";
            const string otherInjectorNamespace = "inj-ns-b";
            var injectorB = SetupResolvableBundle(state, hasher, ReconcilePolicy.OnCreate, "new-hash",
                otherInjectorName, otherInjectorNamespace);

            await handler.Handle(new InjectorMatched(target, injectorB), CancellationToken.None);

            await patcher.Received().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }

        [Fact]
        public async Task Handle_defers_when_binding_still_valid_but_injector_transiently_not_ready_under_OnCreate()
        {
            // Same gate path as Handle_defers_patch_when_annotated_injector_is_OnCreate,
            // but named/commented to make the TRANSIENT-not-ready scenario explicit and
            // distinct from a genuine un-match. The annotated injector is present,
            // enabled, OnCreate, and still selector-matches this workload — the binding
            // is fully valid. The notification's injector is null only because it is
            // momentarily not ready (e.g. dependent resources not yet resolved
            // elsewhere in the pipeline), not because the workload stopped matching.
            // The gate must preserve the binding rather than stripping it.
            var (handler, state, patcher, _) = CreateGraph();
            var target = AnnotatedTarget();
            state.GetById<AgentInjectorResource>(InjName, InjNamespace, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<AgentInjectorResource?>(EnabledOnCreateInjectorSelectingTarget()));

            await handler.Handle(new InjectorMatched(target, null), CancellationToken.None);

            await patcher.DidNotReceive().Patch<V1Deployment>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<V1Deployment>>());
        }
    }
}
