// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults
{
    public class ClusterAgentConnectionSecretSyncingHandler
        : BaseSyncingHandler<ClusterAgentConnectionResource, SecretResource, V1Secret>
    {
        private readonly IStateContainer _state;
        private readonly IKubernetesClient _kubernetesClient;
        private readonly ClusterDefaults _clusterDefaults;
        private readonly IGlobMatcher _matcher;

        protected override string EntityName => "AgentConnectionSecret";

        public ClusterAgentConnectionSecretSyncingHandler(IStateContainer state,
                                                          OperatorOptions operatorOptions,
                                                          IResourceComparer comparer,
                                                          IKubernetesClient kubernetesClient,
                                                          ClusterDefaults clusterDefaults,
                                                          IReactionHelper reactionHelper,
                                                          IGlobMatcher matcher)
            : base(state, operatorOptions, comparer, kubernetesClient, clusterDefaults, reactionHelper)
        {
            _state = state;
            _kubernetesClient = kubernetesClient;
            _clusterDefaults = clusterDefaults;
            _matcher = matcher;
        }

        protected override Task<ResourceIdentityPair<ClusterAgentConnectionResource>?> GetBestBaseForNamespace(
            IEnumerable<ResourceIdentityPair<ClusterAgentConnectionResource>> clusterResources,
            string @namespace)
        {
            var matchingDefaultBase = clusterResources.Where(x => x.Resource.NamespacePatterns.Count == 0
                                                                  || x.Resource.NamespacePatterns.Any(pattern => _matcher.Matches(pattern, @namespace)))
                                                      .ToList();
            if (matchingDefaultBase.Count > 1)
            {
                Logger.Warn($"Multiple {EntityName} entities "
                            + $"[{string.Join(", ", matchingDefaultBase.Select(x => x.Identity.Name))}] match the namespace '{@namespace}'. "
                            + "Selecting first alphabetically to solve for ambiguity.");
                return Task.FromResult(matchingDefaultBase.OrderBy(x => x.Identity.Name).First())!;
            }

            return Task.FromResult(matchingDefaultBase.SingleOrDefault());
        }

        protected override async Task<SecretResource?> CreateDesiredResource(ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
                                                                             string targetName,
                                                                             string targetNamespace)
        {
            var @namespace = baseResource.Identity.Namespace;
            var template = baseResource.Resource.Template;

            var usernameHash = await GetDataHashByRef(template.UserName.Name, @namespace, template.UserName.Key);
            var apiKeyHash = await GetDataHashByRef(template.ApiKey.Name, @namespace, template.ApiKey.Key);
            var serviceKeyHash = await GetDataHashByRef(template.ServiceKey.Name, @namespace, template.ServiceKey.Key);

            if (usernameHash == null
                || apiKeyHash == null
                || serviceKeyHash == null)
            {
                return null;
            }

            var secretKeyValues = new List<SecretKeyValue>
            {
                new(ClusterDefaultsConstants.DefaultUsernameSecretKey, usernameHash),
                new(ClusterDefaultsConstants.DefaultApiKeySecretKey, apiKeyHash),
                new(ClusterDefaultsConstants.DefaultServiceKeySecretKey, serviceKeyHash),
            };
            return new SecretResource(secretKeyValues.NormalizeSecrets());
        }

        protected override async Task<V1Secret?> CreateTargetEntity(ResourceIdentityPair<ClusterAgentConnectionResource> baseResource,
                                                                    SecretResource desiredResource,
                                                                    string targetName,
                                                                    string targetNamespace)
        {
            var @namespace = baseResource.Identity.Namespace;
            var template = baseResource.Resource.Template;

            var usernameData = await GetLiveDataByRef(template.UserName.Name, @namespace, template.UserName.Key);
            var apiKeyData = await GetLiveDataByRef(template.ApiKey.Name, @namespace, template.ApiKey.Key);
            var serviceKeyData = await GetLiveDataByRef(template.ServiceKey.Name, @namespace, template.ServiceKey.Key);

            if (usernameData == null
                || apiKeyData == null
                || serviceKeyData == null)
            {
                return null;
            }

            return new V1Secret(
                metadata: new V1ObjectMeta
                {
                    Name = targetName,
                    NamespaceProperty = targetNamespace
                },
                data: new Dictionary<string, byte[]>
                {
                    { ClusterDefaultsConstants.DefaultUsernameSecretKey, usernameData },
                    { ClusterDefaultsConstants.DefaultApiKeySecretKey, apiKeyData },
                    { ClusterDefaultsConstants.DefaultServiceKeySecretKey, serviceKeyData }
                }
            );
        }

        protected override string GetTargetEntityName(string targetNamespace)
        {
            return _clusterDefaults.GetDefaultAgentConnectionSecretName(targetNamespace);
        }

        private async Task<string?> GetDataHashByRef(string name, string @namespace, string key)
        {
            return (await _state.GetById<SecretResource>(name, @namespace))
                   ?.KeyPairs
                   .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
                   ?.DataHash;
        }

        private async Task<byte[]?> GetLiveDataByRef(string name, string @namespace, string key)
        {
            var liveSecret = await _kubernetesClient.Get<V1Secret>(name, @namespace);
            if (liveSecret?.Data != null
                && liveSecret.Data.TryGetValue(key, out var value)
                && value != null)

            {
                return value;
            }

            return null;
        }
    }
}
