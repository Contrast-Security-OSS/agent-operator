// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public class ClusterAgentConfigurationSyncingHandler
    : BaseTemplateSyncingHandler<ClusterAgentConfigurationResource, AgentConfigurationResource, V1Beta1AgentConfiguration>
{
    private readonly ClusterDefaults _clusterDefaults;

    protected override string EntityName => "AgentConfiguration";

    public ClusterAgentConfigurationSyncingHandler(IStateContainer state,
                                                   IGlobMatcher matcher,
                                                   OperatorOptions operatorOptions,
                                                   IResourceComparer comparer,
                                                   IKubernetesClient kubernetesClient,
                                                   ClusterDefaults clusterDefaults,
                                                   IReactionHelper reactionHelper)
        : base(state, matcher, operatorOptions, comparer, kubernetesClient, clusterDefaults, reactionHelper)
    {
        _clusterDefaults = clusterDefaults;
    }

    protected override ValueTask<V1Beta1AgentConfiguration?> CreateTargetEntity(ResourceIdentityPair<ClusterAgentConfigurationResource> baseResource,
                                                                                AgentConfigurationResource desiredResource,
                                                                                string targetName,
                                                                                string targetNamespace)
    {
        var builder = new StringBuilder();
        foreach (var yamlKey in desiredResource.YamlKeys)
        {
            // Hard code the new line for Linux.
            builder.Append(yamlKey.Key).Append(": ").Append(yamlKey.Value).Append('\n');
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
            Metadata = new V1ObjectMeta(name: targetName, namespaceProperty: targetNamespace),
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

    protected override string GetTargetEntityName(string targetNamespace)
    {
        return _clusterDefaults.GetDefaultAgentConfigurationName(targetNamespace);
    }
}
