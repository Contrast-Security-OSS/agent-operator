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
/// Syncs AgentConfiguration referenced in a ClusterAgentInjector to another namespace
/// </summary>
public class ClusterAgentInjectorConfigurationSyncingHandler
    : BaseAgentInjectorSyncingHandler<AgentConfigurationResource, V1Beta1AgentConfiguration>
{
    private readonly IStateContainer _state;
    private readonly ConfigurationSyncing _configurationSyncing;

    protected override string EntityName => "AgentInjectorConfiguration";

    public ClusterAgentInjectorConfigurationSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher,
        ConfigurationSyncing configurationSyncing)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _state = state;
        _configurationSyncing = configurationSyncing;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return ClusterDefaults.AgentInjectorConfigurationName(targetNamespace, agentType);
    }

    protected override async ValueTask<AgentConfigurationResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var template = baseResource.Resource.Template;

        if (template.ConfigurationReference != null)
        {
            var agentConfiguration = await _state.GetById<AgentConfigurationResource>(template.ConfigurationReference.Name, template.ConfigurationReference.Namespace);
            if (agentConfiguration != null)
            {
                return agentConfiguration;
            }
            else
            {
                Logger.Warn($"Failed to find AgentConfiguration '{template.ConfigurationReference.Namespace}/{template.ConfigurationReference.Name} referenced by {baseResource.Identity.Namespace}/{baseResource.Identity.Name}'");
            }
        }

        return null;
    }

    protected override ValueTask<V1Beta1AgentConfiguration?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
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
