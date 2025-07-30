// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;
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
    private readonly ClusterDefaults _clusterDefaults;
    private readonly IAgentInjectionTypeConverter _typeConverter;

    protected override string EntityName => "AgentInjector";

    public ClusterAgentInjectorSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaults clusterDefaults,
        IResourceComparer comparer,
        IGlobMatcher matcher,
        IAgentInjectionTypeConverter typeConverter)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _clusterDefaults = clusterDefaults;
        _typeConverter = typeConverter;
    }

    protected override ValueTask<AgentInjectorResource?> CreateDesiredResource(
        AgentInjectorResource? existingResource,
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var template = baseResource.Resource.Template;

        var pullSecret = template.ImagePullSecret != null
            ? new SecretReference(targetNamespace,
                _clusterDefaults.GetDefaultPullSecretName(targetNamespace, template.Type), ".dockerconfigjson")
            : null;

        var resource = new AgentInjectorResource(
            template.Enabled,
            template.Type,
            template.Image,
            template.Selector with { Namespaces = new List<string> { targetNamespace } },
            existingResource?.ConnectionReference ?? new AgentInjectorConnectionReference(targetNamespace, _clusterDefaults.GetDefaultAgentConnectionName(targetNamespace), true),
            existingResource?.ConfigurationReference ?? new AgentConfigurationReference(targetNamespace, _clusterDefaults.GetDefaultAgentConfigurationName(targetNamespace), true),
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
        var spec = new V1Beta1AgentInjector.AgentInjectorSpec
        {
            Enabled = desiredResource.Enabled,
            Version = desiredResource.Image.Tag,
            Type = _typeConverter.GetStringFromType(desiredResource.Type),
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
                Labels = desiredResource.Selector.LabelPatterns.Select(x => new V1Beta1AgentInjector.LabelSelectorSpec
                    { Name = x.Key, Value = x.Value }).ToList()
            },
            Connection = new V1Beta1AgentInjector.AgentInjectorConnectionSpec
            {
                Name = desiredResource.ConnectionReference.Name
            },
            Configuration = new V1Beta1AgentInjector.AgentInjectorConfigurationSpec
            {
                Name = desiredResource.ConfigurationReference.Name
            }
        };

        return ValueTask.FromResult(new V1Beta1AgentInjector
        {
            Metadata = new V1ObjectMeta(name: targetName, namespaceProperty: targetNamespace),
            Spec = spec
        })!;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return _clusterDefaults.GetDefaultAgentInjectorName(targetNamespace, agentType);
    }
}
