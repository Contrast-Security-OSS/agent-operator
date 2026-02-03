// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using KubeOps.KubernetesClient;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

/// <summary>
/// Syncs ImagePullSecret Secret referenced in a ClusterAgentInjector to namespaced Secret
/// </summary>
public class ClusterAgentInjectorPullSecretSyncingHandler
    : BaseAgentInjectorSyncingHandler<SecretResource, V1Secret>
{
    private readonly ISecretHelper _secretHelper;

    protected override string EntityName => "AgentInjectorPullSecret";

    public ClusterAgentInjectorPullSecretSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher,
        ISecretHelper secretHelper)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _secretHelper = secretHelper;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return ClusterDefaults.AgentInjectorPullSecretName(targetNamespace, agentType);
    }

    protected override async ValueTask<SecretResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var @namespace = baseResource.Identity.Namespace;
        var template = baseResource.Resource.Template;

        if (template.ImagePullSecret != null)
        {
            var pullSecretHash = await _secretHelper.GetCachedSecretDataHashByRef(template.ImagePullSecret.Name, @namespace, template.ImagePullSecret.Key);
            if (pullSecretHash != null)
            {
                return new SecretResource(new List<SecretKeyValue> { new(".dockerconfigjson", pullSecretHash) });
            }
        }

        return null;
    }

    protected override async ValueTask<V1Secret?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        SecretResource desiredResource,
        string targetName,
        string targetNamespace)
    {
        var @namespace = baseResource.Identity.Namespace;
        var template = baseResource.Resource.Template;

        if (template.ImagePullSecret == null)
        {
            return null;
        }

        var pullSecretData = await _secretHelper.GetLiveSecretDataByRef(template.ImagePullSecret.Name, @namespace, template.ImagePullSecret.Key);
        if (pullSecretData == null)
        {
            return null;
        }

        return new V1Secret
        {
            Metadata = new V1ObjectMeta
            {
                Name = targetName,
                NamespaceProperty = targetNamespace,
            },
            Data = new Dictionary<string, byte[]> { { ".dockerconfigjson", pullSecretData } },
            Type = "kubernetes.io/dockerconfigjson"
        };
    }
}
