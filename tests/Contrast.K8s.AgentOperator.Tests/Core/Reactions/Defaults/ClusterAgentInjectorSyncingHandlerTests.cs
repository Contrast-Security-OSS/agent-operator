// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using FluentAssertions;
using KubeOps.KubernetesClient;
using NSubstitute;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Defaults
{
    public class ClusterAgentInjectorSyncingHandlerTests
    {
        private static readonly Fixture AutoFixture = new();

        // Exposes the protected transforms. The base ctor only stores its
        // dependencies (BaseSyncingHandler.cs:45-53), and the two methods under
        // test use no injected dependency, so unused deps are passed as null.
        private class TestableHandler : ClusterAgentInjectorSyncingHandler
        {
            public TestableHandler(ClusterDefaultsHelper clusterDefaults)
                : base(Substitute.For<IStateContainer>(), null!, Substitute.For<IKubernetesClient>(),
                    Substitute.For<IReactionHelper>(), clusterDefaults, Substitute.For<IResourceComparer>(), null!)
            {
            }

            public ValueTask<AgentInjectorResource?> CallCreateDesired(
                ResourceIdentityPair<ClusterAgentInjectorResource> b, string n, string ns) =>
                CreateDesiredResource(b, n, ns);

            public ValueTask<Contrast.K8s.AgentOperator.Entities.V1Beta1AgentInjector?> CallCreateTarget(
                ResourceIdentityPair<ClusterAgentInjectorResource> b, AgentInjectorResource desired, string n, string ns) =>
                CreateTargetEntity(b, desired, n, ns);
        }

        [Fact]
        public async Task CreateDesiredResource_carries_reconcile_policy_from_template()
        {
            var template = AutoFixture.Create<AgentInjectorResource>() with { ReconcilePolicy = ReconcilePolicy.OnCreate };
            var clusterResource = AutoFixture.Create<ClusterAgentInjectorResource>() with { Template = template };
            var identity = NamespacedResourceIdentity.Create<ClusterAgentInjectorResource>("c", "ns");
            var pair = new ResourceIdentityPair<ClusterAgentInjectorResource>(identity, clusterResource);
            var handler = new TestableHandler(new ClusterDefaultsHelper(Substitute.For<IStateContainer>()));

            var result = await handler.CallCreateDesired(pair, "target", "target-ns");

            result!.ReconcilePolicy.Should().Be(ReconcilePolicy.OnCreate);
        }

        [Fact]
        public async Task CreateTargetEntity_writes_reconcile_policy_string()
        {
            var clusterResource = AutoFixture.Create<ClusterAgentInjectorResource>();
            var identity = NamespacedResourceIdentity.Create<ClusterAgentInjectorResource>("c", "ns");
            var pair = new ResourceIdentityPair<ClusterAgentInjectorResource>(identity, clusterResource);
            var desired = AutoFixture.Create<AgentInjectorResource>() with { ReconcilePolicy = ReconcilePolicy.OnCreate };
            var handler = new TestableHandler(new ClusterDefaultsHelper(Substitute.For<IStateContainer>()));

            var result = await handler.CallCreateTarget(pair, desired, "target", "target-ns");

            result!.Spec.ReconcilePolicy.Should().Be("OnCreate");
        }
    }
}
