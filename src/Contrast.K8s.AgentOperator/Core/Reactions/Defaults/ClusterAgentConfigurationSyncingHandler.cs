﻿using System.Text;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults
{
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

        protected override Task<V1Beta1AgentConfiguration?> CreateTargetEntity(ResourceIdentityPair<ClusterAgentConfigurationResource> baseResource,
                                                                               AgentConfigurationResource desiredResource,
                                                                               string targetName,
                                                                               string targetNamespace)
        {
            var builder = new StringBuilder();
            foreach (var yamlKey in desiredResource.YamlKeys)
            {
                // Hard code the new line to Linux.
                builder.Append(yamlKey.Key).Append(": ").Append(yamlKey.Value).Append("\n");
            }

            var yaml = builder.ToString();

            return Task.FromResult(new V1Beta1AgentConfiguration
            {
                Metadata = new V1ObjectMeta(name: targetName, namespaceProperty: targetNamespace),
                Spec = new V1Beta1AgentConfiguration.AgentConfigurationSpec
                {
                    Yaml = yaml
                }
            })!;
        }

        protected override string GetTargetEntityName(string targetNamespace)
        {
            return _clusterDefaults.GetDefaultAgentConfigurationName(targetNamespace);
        }
    }
}