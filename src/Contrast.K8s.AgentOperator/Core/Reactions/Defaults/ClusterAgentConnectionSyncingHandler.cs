// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

public class ClusterAgentConnectionSyncingHandler
    : BaseUniqueSyncingHandler<ClusterAgentConnectionResource, AgentConnectionResource, V1Beta1AgentConnection>
{

    protected override string EntityName => "AgentConnection";

    public ClusterAgentConnectionSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer, matcher)
    {
    }

    protected override ValueTask<V1Beta1AgentConnection?> CreateTargetEntity(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        AgentConnectionResource desiredResource,
        string targetName,
        string targetNamespace)
    {
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

    protected override string GetTargetEntityName(string targetNamespace)
    {
        return ClusterDefaults.AgentConnectionName(targetNamespace);
    }

    protected override ValueTask<AgentConnectionResource?> CreateDesiredResource(
        ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
        string targetName,
        string targetNamespace)
    {
        var secretName = ClusterDefaults.AgentConnectionSecretName(targetNamespace);
        var template = baseResource.Resource.Template;

        SecretReference? token = null;
        if (template.Token != null)
        {
            token = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultTokenSecretKey);
        }

        SecretReference? apiKey = null;
        if (template.ApiKey != null)
        {
            apiKey = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultApiKeySecretKey);
        }

        SecretReference? serviceKey = null;
        if (template.ServiceKey != null)
        {
            serviceKey = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultServiceKeySecretKey);
        }

        SecretReference? username = null;
        if (template.UserName != null)
        {
            username = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultUsernameSecretKey);
        }

        return ValueTask.FromResult(baseResource.Resource.Template with
        {
            Token = token,
            ApiKey = apiKey,
            ServiceKey = serviceKey,
            UserName = username
        })!;
    }
}
