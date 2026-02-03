// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Common;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using KubeOps.KubernetesClient;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

/// <summary>
/// Syncs ClusterAgentConnection to namespaced AgentConnection
/// </summary>
public class ClusterAgentConnectionSyncingHandler
    : BaseUniqueSyncingHandler<ClusterAgentConnectionResource, AgentConnectionResource, V1Beta1AgentConnection>
{
    private readonly ConnectionSyncing _connectionSyncing;

    protected override string EntityName => "AgentConnection";

    public ClusterAgentConnectionSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher,
        ConnectionSyncing connectionSyncing)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _connectionSyncing = connectionSyncing;
    }

    protected override string GetTargetEntityName(string targetNamespace)
    {
        return ClusterDefaults.AgentConnectionName(targetNamespace);
    }

    protected override ValueTask<AgentConnectionResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var secretName = ClusterDefaults.AgentConnectionSecretName(targetNamespace);
        return ValueTask.FromResult(_connectionSyncing.CreateConnectionResource(baseResource.Resource.Template, secretName, targetNamespace))!;
    }

    protected override ValueTask<V1Beta1AgentConnection?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        AgentConnectionResource desiredResource,
        string targetName,
        string targetNamespace)
    {
        return ValueTask.FromResult(new V1Beta1AgentConnection
        {
            Metadata = new V1ObjectMeta { Name = targetName, NamespaceProperty = targetNamespace },
            Spec = _connectionSyncing.CreateConnectionSpec(desiredResource)
        })!;
    }

}
