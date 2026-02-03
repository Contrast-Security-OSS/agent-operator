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
/// Syncs ClusterAgentConfiguration to namespaced AgentConfiguration
/// </summary>
public class ClusterAgentConfigurationSyncingHandler
    : BaseUniqueSyncingHandler<ClusterAgentConfigurationResource, AgentConfigurationResource, V1Beta1AgentConfiguration>
{
    private readonly ConfigurationSyncing _configurationSyncing;

    protected override string EntityName => "AgentConfiguration";

    public ClusterAgentConfigurationSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher,
        ConfigurationSyncing configurationSyncing)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _configurationSyncing = configurationSyncing;
    }

    protected override string GetTargetEntityName(string targetNamespace)
    {
        return ClusterDefaults.AgentConfigurationName(targetNamespace);
    }

    protected override ValueTask<AgentConfigurationResource?> CreateDesiredResource(
    ResourceIdentityPair<ClusterAgentConfigurationResource> baseResource, string targetName,
    string targetNamespace)
    {
        return ValueTask.FromResult(baseResource.Resource.Template)!;
    }

    protected override ValueTask<V1Beta1AgentConfiguration?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentConfigurationResource> baseResource,
        AgentConfigurationResource desiredResource,
        string targetName,
        string targetNamespace)
    {
        return ValueTask.FromResult(new V1Beta1AgentConfiguration
        {
            Metadata = new V1ObjectMeta { Name = targetName, NamespaceProperty = targetNamespace },
            Spec = _configurationSyncing.CreateConfigurationSpec(desiredResource)
        })!;
    }
}
