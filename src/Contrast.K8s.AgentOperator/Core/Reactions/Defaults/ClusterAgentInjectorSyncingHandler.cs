// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public class ClusterAgentInjectorSyncingHandler
    : BaseAgentInjectorSyncingHandler<AgentInjectorResource, V1Beta1AgentInjector>
{
    protected override string EntityName => "AgentInjector";

    public ClusterAgentInjectorSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
    }

    protected override ValueTask<AgentInjectorResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var template = baseResource.Resource.Template;

        var pullSecret = template.ImagePullSecret != null
            ? new SecretReference(targetNamespace,
                ClusterDefaults.AgentInjectorPullSecretName(targetNamespace, template.Type), ".dockerconfigjson")
            : null;

        var connectionRef = template.ConnectionReference != null
            ? new AgentConnectionReference(targetNamespace, ClusterDefaults.AgentInjectorConnectionName(targetNamespace, template.Type))
            : null;

        var configurationRef = template.ConfigurationReference != null
            ? new AgentConfigurationReference(targetNamespace, ClusterDefaults.AgentInjectorConfigurationName(targetNamespace, template.Type))
            : null;

        var resource = new AgentInjectorResource(
            template.Enabled,
            template.Type,
            template.Image,
            template.Selector with { Namespaces = new List<string> { targetNamespace } },
            connectionRef,
            configurationRef,
            pullSecret,
            template.ImagePullPolicy
        );

        return ValueTask.FromResult(resource)!;
    }

    protected override ValueTask<V1Beta1AgentInjector?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        AgentInjectorResource desiredResource,
        string targetName,
        string targetNamespace)
    {
        var connection = desiredResource.ConnectionReference != null
            ? new V1Beta1AgentInjector.AgentInjectorConnectionSpec { Name = desiredResource.ConnectionReference.Name }
            : null;

        var configuration = desiredResource.ConfigurationReference != null
            ? new V1Beta1AgentInjector.AgentInjectorConfigurationSpec { Name = desiredResource.ConfigurationReference.Name }
            : null;

        var spec = new V1Beta1AgentInjector.AgentInjectorSpec
        {
            Enabled = desiredResource.Enabled,
            Version = desiredResource.Image.Tag,
            Type = AgentInjectionTypeConverter.GetStringFromType(desiredResource.Type),
            Image = new V1Beta1AgentInjector.AgentInjectorImageSpec
            {
                Name = desiredResource.Image.Name,
                Registry = desiredResource.Image.Registry,
                PullPolicy = desiredResource.ImagePullPolicy,
                PullSecretName = desiredResource.ImagePullSecret?.Name
            },
            Selector = new V1Beta1AgentInjector.AgentInjectorSelectorSpec
            {
                Images = desiredResource.Selector.ImagesPatterns,
                Labels = desiredResource.Selector.LabelPatterns.Select(x => new V1Beta1AgentInjector.AgentInjectorLabelSelectorSpec
                    { Name = x.Key, Value = x.Value }).ToList()
            },
            Connection = connection,
            Configuration = configuration
        };

        return ValueTask.FromResult(new V1Beta1AgentInjector
        {
            Metadata = new V1ObjectMeta { Name = targetName, NamespaceProperty = targetNamespace },
            Spec = spec
        })!;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return ClusterDefaults.AgentInjectorName(targetNamespace, agentType);
    }
}
