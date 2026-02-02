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

public class ClusterAgentConnectionSecretSyncingHandler
    : BaseUniqueSyncingHandler<ClusterAgentConnectionResource, SecretResource, V1Secret>
{
    private readonly ISecretHelper _secretHelper;

    protected override string EntityName => "AgentConnectionSecret";

    public ClusterAgentConnectionSecretSyncingHandler(IStateContainer state,
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

    protected override async ValueTask<SecretResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var @namespace = baseResource.Identity.Namespace;
        var template = baseResource.Resource.Template;

        var secretKeyValues = new List<SecretKeyValue>();

        if (template.Token != null)
        {
            var tokenHash = await _secretHelper.GetCachedSecretDataHashByRef(template.Token.Name, @namespace, template.Token.Key);
            if (tokenHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultTokenSecretKey, tokenHash));
            }
        }

        if (template.UserName != null)
        {
            var usernameHash = await _secretHelper.GetCachedSecretDataHashByRef(template.UserName.Name, @namespace, template.UserName.Key);
            if (usernameHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultUsernameSecretKey, usernameHash));
            }
        }

        if (template.ApiKey != null)
        {
            var apiKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(template.ApiKey.Name, @namespace, template.ApiKey.Key);
            if (apiKeyHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultApiKeySecretKey, apiKeyHash));
            }
        }

        if (template.ServiceKey != null)
        {
            var serviceKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(template.ServiceKey.Name, @namespace, template.ServiceKey.Key);
            if (serviceKeyHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultServiceKeySecretKey, serviceKeyHash));
            }
        }

        return new SecretResource(secretKeyValues.NormalizeSecrets());
    }

    protected override async ValueTask<V1Secret?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        SecretResource desiredResource,
        string targetName,
        string targetNamespace)
    {
        var @namespace = baseResource.Identity.Namespace;
        var template = baseResource.Resource.Template;

        var data = new Dictionary<string, byte[]>();

        if (template.Token != null)
        {
            var tokenData = await _secretHelper.GetLiveSecretDataByRef(template.Token.Name, @namespace, template.Token.Key);
            if (tokenData != null)
            {
                data.Add(ClusterDefaults.DefaultTokenSecretKey, tokenData);
            }
        }

        if (template.UserName != null)
        {
            var usernameData = await _secretHelper.GetLiveSecretDataByRef(template.UserName.Name, @namespace, template.UserName.Key);
            if (usernameData != null)
            {
                data.Add(ClusterDefaults.DefaultUsernameSecretKey, usernameData);
            }
        }

        if (template.ApiKey != null)
        {
            var apiKeyData = await _secretHelper.GetLiveSecretDataByRef(template.ApiKey.Name, @namespace, template.ApiKey.Key);
            if (apiKeyData != null)
            {
                data.Add(ClusterDefaults.DefaultApiKeySecretKey, apiKeyData);
            }
        }

        if (template.ServiceKey != null)
        {
            var serviceKeyData = await _secretHelper.GetLiveSecretDataByRef(template.ServiceKey.Name, @namespace, template.ServiceKey.Key);
            if (serviceKeyData != null)
            {
                data.Add(ClusterDefaults.DefaultServiceKeySecretKey, serviceKeyData);
            }
        }

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

    protected override string GetTargetEntityName(string targetNamespace)
    {
        return ClusterDefaults.AgentConnectionSecretName(targetNamespace);
    }
}
