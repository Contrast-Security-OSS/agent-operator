// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults
{
    public abstract class BaseTemplateSyncingHandler<TClusterResource, TTargetResource, TEntity>
        : BaseSyncingHandler<TClusterResource, TTargetResource, TEntity>
        where TClusterResource : class, IClusterResourceTemplate<TTargetResource>
        where TTargetResource : class, INamespacedResource, IMutableResource
        where TEntity : class, IKubernetesObject<V1ObjectMeta>
    {
        private readonly IGlobMatcher _matcher;

        protected BaseTemplateSyncingHandler(IStateContainer state,
                                             IGlobMatcher matcher,
                                             OperatorOptions operatorOptions,
                                             IResourceComparer comparer,
                                             IKubernetesClient kubernetesClient,
                                             ClusterDefaults clusterDefaults,
                                             IReactionHelper reactionHelper)
            : base(state, operatorOptions, comparer, kubernetesClient, clusterDefaults, reactionHelper)
        {
            _matcher = matcher;
        }

        protected override ValueTask<ResourceIdentityPair<TClusterResource>?> GetBestBaseForNamespace(
            IEnumerable<ResourceIdentityPair<TClusterResource>> clusterResources,
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
                return ValueTask.FromResult(matchingDefaultBase.OrderBy(x => x.Identity.Name).First())!;
            }

            return ValueTask.FromResult(matchingDefaultBase.SingleOrDefault());
        }

        protected override ValueTask<TTargetResource?> CreateDesiredResource(ResourceIdentityPair<TClusterResource> baseResource, string targetName,
                                                                             string targetNamespace)
        {
            return ValueTask.FromResult(baseResource.Resource.Template)!;
        }
    }
}
