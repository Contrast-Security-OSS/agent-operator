// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using KubeOps.KubernetesClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public class ClusterAgentInjectorConnectionSecretSyncingHandler
    : BaseAgentInjectorSyncingHandler<SecretResource, V1Secret>
{
    private readonly IStateContainer _state;
    private readonly IKubernetesClient _kubernetesClient;
    private readonly ISecretHelper _secretHelper;

    protected override string EntityName => "AgentInjectorConnectionSecret";

    public ClusterAgentInjectorConnectionSecretSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher,
        ISecretHelper secretHelper)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
        _state = state;
        _kubernetesClient = kubernetesClient;
        _secretHelper = secretHelper;
    }

    protected override async ValueTask<SecretResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentInjectorResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var @namespace = baseResource.Identity.Namespace;
        var template = baseResource.Resource.Template;

        if (template.ConnectionReference != null)
        {
            var agentConnection = await _state.GetById<AgentConnectionResource>(template.ConnectionReference.Name, template.ConnectionReference.Namespace);
            if (agentConnection != null)
            {
                //TODO duplicate of ClusterAgentConnectionSecretSyncingHandler
                var secretKeyValues = new List<SecretKeyValue>();

                if (agentConnection.Token != null)
                {
                    var tokenHash = await _secretHelper.GetCachedSecretDataHashByRef(agentConnection.Token.Name, @namespace, agentConnection.Token.Key);
                    if (tokenHash != null)
                    {
                        secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultTokenSecretKey, tokenHash));
                    }
                }

                if (agentConnection.UserName != null)
                {
                    var usernameHash = await _secretHelper.GetCachedSecretDataHashByRef(agentConnection.UserName.Name, @namespace, agentConnection.UserName.Key);
                    if (usernameHash != null)
                    {
                        secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultUsernameSecretKey, usernameHash));
                    }
                }

                if (agentConnection.ApiKey != null)
                {
                    var apiKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(agentConnection.ApiKey.Name, @namespace, agentConnection.ApiKey.Key);
                    if (apiKeyHash != null)
                    {
                        secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultApiKeySecretKey, apiKeyHash));
                    }
                }

                if (agentConnection.ServiceKey != null)
                {
                    var serviceKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(agentConnection.ServiceKey.Name, @namespace, agentConnection.ServiceKey.Key);
                    if (serviceKeyHash != null)
                    {
                        secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultServiceKeySecretKey, serviceKeyHash));
                    }
                }

                return new SecretResource(secretKeyValues.NormalizeSecrets());
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

        if (template.ConnectionReference != null)
        {

            var agentConnection = await _state.GetById<AgentConnectionResource>(template.ConnectionReference.Name, template.ConnectionReference.Namespace);
            //TODO logging
            if (agentConnection != null)
            {
                //TODO duplicate of ClusterAgentConnectionSecretSyncingHandler
                var data = new Dictionary<string, byte[]>();

                if (agentConnection.Token != null)
                {
                    var tokenData = await _secretHelper.GetLiveSecretDataByRef(agentConnection.Token.Name, @namespace, agentConnection.Token.Key);
                    if (tokenData != null)
                    {
                        data.Add(ClusterDefaults.DefaultTokenSecretKey, tokenData);
                    }
                }

                if (agentConnection.UserName != null)
                {
                    var usernameData = await _secretHelper.GetLiveSecretDataByRef(agentConnection.UserName.Name, @namespace, agentConnection.UserName.Key);
                    if (usernameData != null)
                    {
                        data.Add(ClusterDefaults.DefaultUsernameSecretKey, usernameData);
                    }
                }

                if (agentConnection.ApiKey != null)
                {
                    var apiKeyData = await _secretHelper.GetLiveSecretDataByRef(agentConnection.ApiKey.Name, @namespace, agentConnection.ApiKey.Key);
                    if (apiKeyData != null)
                    {
                        data.Add(ClusterDefaults.DefaultApiKeySecretKey, apiKeyData);
                    }
                }

                if (agentConnection.ServiceKey != null)
                {
                    var serviceKeyData = await _secretHelper.GetLiveSecretDataByRef(agentConnection.ServiceKey.Name, @namespace, agentConnection.ServiceKey.Key);
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
        }
        return null;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return ClusterDefaults.AgentInjectorConnectionSecretName(targetNamespace, agentType);
    }
}
