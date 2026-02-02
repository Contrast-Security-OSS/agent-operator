// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using KubeOps.KubernetesClient;
using System.Text;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public class ClusterAgentInjectorConfigurationSyncingHandler
    : BaseAgentInjectorSyncingHandler<AgentConfigurationResource, V1Beta1AgentConfiguration>
{
    private readonly IStateContainer _state;

    protected override string EntityName => "AgentInjectorConfiguration";

    public ClusterAgentInjectorConfigurationSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _state = state;
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
                return agentConfiguration with { }; //TODO I think this needs to be a deepclone
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
        //TODO duplicate of ClusterAgentConfigurationSyncingHandler
        var builder = new StringBuilder();
        foreach (var yamlKey in desiredResource.YamlKeys)
        {
            // Hard code the new line for Linux.
            builder.Append(yamlKey.Key).Append(": '").Append(yamlKey.Value).Append("'\n");
        }

        var yaml = builder.ToString();

        var initContainer = desiredResource.InitContainerOverrides is { } overrides
            ? new V1Beta1AgentConfiguration.InitContainerOverridesSpec
            {
                SecurityContext = overrides.SecurityContext
            }
            : null;

        return ValueTask.FromResult(new V1Beta1AgentConfiguration
        {
            Metadata = new V1ObjectMeta { Name = targetName, NamespaceProperty = targetNamespace },
            Spec = new V1Beta1AgentConfiguration.AgentConfigurationSpec
            {
                Yaml = yaml,
                SuppressDefaultApplicationName = desiredResource.SuppressDefaultApplicationName,
                SuppressDefaultServerName = desiredResource.SuppressDefaultServerName,
                EnableYamlVariableReplacement = desiredResource.EnableYamlVariableReplacement,
                InitContainer = initContainer
            }
        })!;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return ClusterDefaults.AgentInjectorConfigurationName(targetNamespace, agentType);
    }
}
