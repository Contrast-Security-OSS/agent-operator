// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Common;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using KubeOps.KubernetesClient;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

/// <summary>
/// Syncs AgentConnection referenced in a ClusterAgentInjector to another namespace
/// </summary>
public class ClusterAgentInjectorConnectionSyncingHandler
    : BaseAgentInjectorSyncingHandler<AgentConnectionResource, V1Beta1AgentConnection>
{
    private readonly IStateContainer _state;
    private readonly ConnectionSyncing _connectionSyncing;

    protected override string EntityName => "AgentInjectorConnection";

    public ClusterAgentInjectorConnectionSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher,
        ConnectionSyncing connectionSyncing)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _state = state;
        _connectionSyncing = connectionSyncing;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return ClusterDefaults.AgentInjectorConnectionName(targetNamespace, agentType);
    }

    protected override async ValueTask<AgentConnectionResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var template = baseResource.Resource.Template;

        if (template.ConnectionReference != null)
        {
            var agentConnection = await _state.GetById<AgentConnectionResource>(template.ConnectionReference.Name, template.ConnectionReference.Namespace);
            if (agentConnection != null)
            {
                var secretName = ClusterDefaults.AgentInjectorConnectionSecretName(targetNamespace, template.Type);
                return _connectionSyncing.CreateConnectionResource(agentConnection, secretName, targetNamespace);

            }
            else
            {
                Logger.Warn($"Failed to find AgentConnection '{template.ConnectionReference.Namespace}/{template.ConnectionReference.Name}' referenced by '{baseResource.Identity.Namespace}/{baseResource.Identity.Name}'");
            }
        }

        return null;
    }

    protected override ValueTask<V1Beta1AgentConnection?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
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
