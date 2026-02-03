// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Common;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using KubeOps.KubernetesClient;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

/// <summary>
/// Syncs Secret referenced in a ClusterAgentConnection to namespaced Secret
/// </summary>
public class ClusterAgentConnectionSecretSyncingHandler
    : BaseUniqueSyncingHandler<ClusterAgentConnectionResource, SecretResource, V1Secret>
{
    private readonly ConnectionSyncing _connectionSyncing;

    protected override string EntityName => "AgentConnectionSecret";

    public ClusterAgentConnectionSecretSyncingHandler(IStateContainer state,
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
        return ClusterDefaults.AgentConnectionSecretName(targetNamespace);
    }

    protected override async ValueTask<SecretResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        return await _connectionSyncing.CreateConnectionSecretResource(baseResource.Resource.Template, baseResource.Identity.Namespace);
    }

    protected override async ValueTask<V1Secret?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        SecretResource desiredResource,
        string targetName,
        string targetNamespace)
    {
        var data = await _connectionSyncing.CreateConnectionSecretData(baseResource.Resource.Template, baseResource.Identity.Namespace);

        return new V1Secret
        {
            Metadata = new V1ObjectMeta
            {
                Name = targetName,
                NamespaceProperty = targetNamespace
            },
            Data = data
        };
    }


}
