// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
/// Base for 1 to 1 syncing of cluster resources to namespaces with AgentInjectors
/// </summary>
/// <typeparam name="TClusterResource"></typeparam>
/// <typeparam name="TTargetResource"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public abstract class BaseUniqueSyncingHandler<TClusterResource, TTargetResource, TEntity>
    : BaseSyncingHandler<TClusterResource, TTargetResource, TEntity>
    where TClusterResource : class, INamespacedResource
    where TTargetResource : class, INamespacedResource, IMutableResource
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    private readonly IResourceComparer _comparer;
    private readonly IStateContainer _state;
    private readonly ClusterDefaults _clusterDefaults;

    protected BaseUniqueSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaults clusterDefaults,
        IResourceComparer comparer)
        : base(state, operatorOptions, kubernetesClient, reactionHelper)
    {
        _state = state;
        _clusterDefaults = clusterDefaults;
        _comparer = comparer;
    }

    protected override async ValueTask Sync(CancellationToken cancellationToken)
    {
        var allNamespaces = await _clusterDefaults.GetAllNamespaces(cancellationToken);
        var validNamespaces = await _clusterDefaults.GetValidNamespacesForDefaults(cancellationToken);
        var availableClusterResources = await GetAvailableClusterResources(cancellationToken);

        Logger.Trace(
            $"Checking for cluster '{EntityName}' eligible for generation across {availableClusterResources.Count} templates in {allNamespaces.Count} namespaces.");

        foreach (var targetNamespace in allNamespaces)
        {
            var targetEntityName = GetTargetEntityName(targetNamespace);
            if (await _state.GetIsDirty<TTargetResource>(targetEntityName, targetNamespace, cancellationToken))
            {
                Logger.Trace($"Ignoring dirty '{EntityName}' '{targetNamespace}/{targetEntityName}'.");
                continue;
            }

            var existingResource = await _state.GetById<TTargetResource>(targetEntityName, targetNamespace, cancellationToken);
            var isValidNamespace = validNamespaces.Any(x => string.Equals(x, targetNamespace, StringComparison.OrdinalIgnoreCase));

            if (isValidNamespace
                && await GetBestBaseForNamespace(availableClusterResources, targetNamespace) is { } bestBase
                && await CreateDesiredResource(existingResource, bestBase, targetEntityName, targetNamespace) is { } desiredResource)
            {
                if (!_comparer.AreEqual(existingResource, desiredResource))
                {
                    // Should update.
                    await CreateOrUpdate(bestBase, targetEntityName, targetNamespace, desiredResource, cancellationToken);
                }
            }
            else
            {
                if (existingResource != null)
                {
                    // Should delete.
                    await Delete(targetEntityName, targetNamespace, cancellationToken);
                }
            }
        }
    }

    protected abstract ValueTask<ResourceIdentityPair<TClusterResource>?> GetBestBaseForNamespace(
        IEnumerable<ResourceIdentityPair<TClusterResource>> clusterResources,
        string @namespace);

    protected abstract string GetTargetEntityName(string targetNamespace);
}
