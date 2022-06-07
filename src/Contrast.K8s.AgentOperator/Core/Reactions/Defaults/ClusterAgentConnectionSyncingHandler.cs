using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults
{
    public class ClusterAgentConnectionSyncingHandler
        : BaseTemplateSyncingHandler<ClusterAgentConnectionResource, AgentConnectionResource, V1Beta1AgentConnection>
    {
        private readonly ClusterDefaults _clusterDefaults;

        protected override string EntityName => "AgentConnection";

        public ClusterAgentConnectionSyncingHandler(IStateContainer state,
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

        protected override Task<V1Beta1AgentConnection?> CreateTargetEntity(ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
                                                                            AgentConnectionResource desiredResource,
                                                                            string targetName,
                                                                            string targetNamespace)
        {
            return Task.FromResult(new V1Beta1AgentConnection
            {
                Metadata = new V1ObjectMeta(name: targetName, namespaceProperty: targetNamespace),
                Spec = new V1Beta1AgentConnection.AgentInjectorSpec
                {
                    Url = desiredResource.TeamServerUri,
                    UserName = new V1Beta1AgentConnection.SecretRef
                    {
                        SecretName = desiredResource.UserName.Name,
                        SecretKey = desiredResource.UserName.Key
                    },
                    ServiceKey = new V1Beta1AgentConnection.SecretRef
                    {
                        SecretName = desiredResource.ServiceKey.Name,
                        SecretKey = desiredResource.ServiceKey.Key
                    },
                    ApiKey = new V1Beta1AgentConnection.SecretRef
                    {
                        SecretName = desiredResource.ApiKey.Name,
                        SecretKey = desiredResource.ApiKey.Key
                    }
                }
            })!;
        }

        protected override string GetTargetEntityName(string targetNamespace)
        {
            return _clusterDefaults.GetDefaultAgentConnectionName(targetNamespace);
        }

        protected override Task<AgentConnectionResource?> CreateDesiredResource(ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
                                                                                string targetName,
                                                                                string targetNamespace)
        {
            var secretName = _clusterDefaults.GetDefaultAgentConnectionSecretName(targetNamespace);
            return Task.FromResult(baseResource.Resource.Template with
            {
                UserName = baseResource.Resource.Template.UserName with
                {
                    Name = secretName,
                    Key = ClusterDefaultsConstants.DefaultUsernameSecretKey,
                    Namespace = targetNamespace
                },
                ServiceKey = baseResource.Resource.Template.ServiceKey with
                {
                    Name = secretName,
                    Key = ClusterDefaultsConstants.DefaultServiceKeySecretKey,
                    Namespace = targetNamespace
                },
                ApiKey = baseResource.Resource.Template.ApiKey with
                {
                    Name = secretName,
                    Key = ClusterDefaultsConstants.DefaultApiKeySecretKey,
                    Namespace = targetNamespace
                }
            })!;
        }
    }
}
