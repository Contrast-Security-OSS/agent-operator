// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;

/// <summary>
/// Base for syncing cluster resources based on AgentType
/// </summary>
/// <typeparam name="TTargetResource"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public abstract class BaseAgentInjectorSyncingHandler<TTargetResource, TEntity>
    : BaseSyncingHandler<ClusterAgentInjectorResource, TTargetResource, TEntity>
    where TTargetResource : class, INamespacedResource, IMutableResource
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    private readonly IStateContainer _state;
    private readonly IResourceComparer _comparer;
    private readonly ClusterResourceMatcher _matcher;
    private readonly ClusterDefaultsHelper _clusterDefaults;

    protected BaseAgentInjectorSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ClusterDefaultsHelper clusterDefaults,
        IResourceComparer comparer,
        ClusterResourceMatcher matcher)
        : base(state, operatorOptions, kubernetesClient, reactionHelper)
    {
        _state = state;
        _comparer = comparer;
        _matcher = matcher;
        _clusterDefaults = clusterDefaults;
    }

    protected override async ValueTask Sync(CancellationToken cancellationToken)
    {
        var allNamespaces = await _clusterDefaults.GetAllNamespaces(cancellationToken);
        var systemNamespaces = _clusterDefaults.GetSystemNamespaces();
        var availableClusterResources = await GetAvailableClusterResources(cancellationToken);

        Logger.Trace(
            $"Checking for cluster '{EntityName}' eligible for generation across {availableClusterResources.Count} templates in {allNamespaces.Count} namespaces.");

        foreach (var targetNamespace in allNamespaces)
        {
            //Skip system namespaces
            if (systemNamespaces.Any(x => string.Equals(x, targetNamespace, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var namespaceResource =
                await _state.GetById<NamespaceResource>(targetNamespace, targetNamespace, cancellationToken);
            if (namespaceResource == null)
            {
                Logger.Debug($"Failed to pull NamespaceResource for {targetNamespace}");
                continue;
            }

            foreach (var agentType in Enum.GetValues<AgentInjectionType>())
            {
                var targetEntityName = GetTargetEntityName(targetNamespace, agentType);
                if (await _state.GetIsDirty<TTargetResource>(targetEntityName, targetNamespace, cancellationToken))
                {
                    Logger.Trace($"Ignoring dirty '{EntityName}' '{targetNamespace}/{targetEntityName}'.");
                    continue;
                }

                var existingResource = await _state.GetById<TTargetResource>(targetEntityName, targetNamespace, cancellationToken);

                if (await GetBestBaseForNamespace(availableClusterResources, targetNamespace, namespaceResource, agentType) is { } bestBase
                    && await CreateDesiredResource(bestBase, targetEntityName, targetNamespace) is { } desiredResource)
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
    }

    protected ValueTask<ResourceIdentityPair<ClusterAgentInjectorResource>?> GetBestBaseForNamespace(
        IEnumerable<ResourceIdentityPair<ClusterAgentInjectorResource>> clusterResources,
        string namespaceName,
        NamespaceResource namespaceResource,
        AgentInjectionType agentType)
    {
        var matchingBases = _matcher.GetMatchingBasesForAgent(clusterResources, namespaceName, namespaceResource, agentType);
        if (matchingBases.Count > 1)
        {
            Logger.Warn($"Multiple {EntityName} entities "
                        + $"[{string.Join(", ", matchingBases.Select(x => x.Identity.Name))}] match the namespace '{namespaceName}'. "
                        + "Selecting first alphabetically to solve for ambiguity.");
            return ValueTask.FromResult(matchingBases.OrderBy(x => x.Identity.Name).First())!;
        }

        return ValueTask.FromResult(matchingBases.SingleOrDefault());
    }

    protected abstract string GetTargetEntityName(string targetNamespace, AgentInjectionType agentType);
}
