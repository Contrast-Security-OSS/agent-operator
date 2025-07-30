// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Options;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;

/// <summary>
/// Base for syncing cluster resources with templates
/// </summary>
/// <typeparam name="TClusterResource"></typeparam>
/// <typeparam name="TTargetResource"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public abstract class BaseTemplateSyncingHandler<TClusterResource, TTargetResource, TEntity>
    : BaseUniqueSyncingHandler<TClusterResource, TTargetResource, TEntity>
    where TClusterResource : class, IClusterResourceTemplate<TTargetResource>
    where TTargetResource : class, INamespacedResource, IMutableResource
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    private readonly IGlobMatcher _matcher;

    protected BaseTemplateSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaults clusterDefaults,
        IResourceComparer comparer,
        IGlobMatcher matcher)
        : base(state, operatorOptions, kubernetesClient, reactionHelper, clusterDefaults, comparer)
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

    protected override ValueTask<TTargetResource?> CreateDesiredResource(TTargetResource? existingResource,
        ResourceIdentityPair<TClusterResource> baseResource, string targetName,
        string targetNamespace)
    {
        return ValueTask.FromResult(baseResource.Resource.Template)!;
    }
}
