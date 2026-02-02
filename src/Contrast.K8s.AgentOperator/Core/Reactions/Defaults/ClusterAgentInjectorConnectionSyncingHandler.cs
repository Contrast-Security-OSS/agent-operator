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
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public class ClusterAgentInjectorConnectionSyncingHandler
    : BaseAgentInjectorSyncingHandler<AgentConnectionResource, V1Beta1AgentConnection>
{
    private readonly IStateContainer _state;

    protected override string EntityName => "AgentInjectorConnection";

    public ClusterAgentInjectorConnectionSyncingHandler(IStateContainer state,
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

                //TODO duplicate of ClusterAgentConnectionSyncingHandler
                SecretReference? token = null;
                if (agentConnection.Token != null)
                {
                    token = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultTokenSecretKey);
                }

                SecretReference? apiKey = null;
                if (agentConnection.ApiKey != null)
                {
                    apiKey = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultApiKeySecretKey);
                }

                SecretReference? serviceKey = null;
                if (agentConnection.ServiceKey != null)
                {
                    serviceKey = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultServiceKeySecretKey);
                }

                SecretReference? username = null;
                if (agentConnection.UserName != null)
                {
                    username = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultUsernameSecretKey);
                }

                return agentConnection with
                {
                    Token = token,
                    ApiKey = apiKey,
                    ServiceKey = serviceKey,
                    UserName = username
                };
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
        //TODO duplicate of ClusterAgentConnectionSyncingHandler
        var spec = new V1Beta1AgentConnection.AgentConnectionSpec
        {
            MountAsVolume = desiredResource.MountAsVolume,
            Url = desiredResource.TeamServerUri
        };

        if (desiredResource.Token != null)
        {
            spec.Token = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.Token.Name,
                SecretKey = desiredResource.Token.Key
            };
        }

        if (desiredResource.ApiKey != null)
        {
            spec.ApiKey = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.ApiKey.Name,
                SecretKey = desiredResource.ApiKey.Key
            };
        }

        if (desiredResource.ServiceKey != null)
        {
            spec.ServiceKey = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.ServiceKey.Name,
                SecretKey = desiredResource.ServiceKey.Key
            };
        }

        if (desiredResource.UserName != null)
        {
            spec.UserName = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.UserName.Name,
                SecretKey = desiredResource.UserName.Key
            };
        }

        return ValueTask.FromResult(new V1Beta1AgentConnection
        {
            Metadata = new V1ObjectMeta { Name = targetName, NamespaceProperty = targetNamespace },
            Spec = spec
        })!;
    }

    protected override string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType)
    {
        return ClusterDefaults.AgentInjectorConnectionName(targetNamespace, agentType);
    }
}
