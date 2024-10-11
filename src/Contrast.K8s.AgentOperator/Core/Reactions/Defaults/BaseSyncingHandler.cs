// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Options;
using k8s;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public abstract class BaseSyncingHandler<TClusterResource, TTargetResource, TEntity>
    : INotificationHandler<DeferredStateModified>
    where TClusterResource : class, INamespacedResource
    where TTargetResource : class, INamespacedResource, IMutableResource
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    // ReSharper disable once InconsistentNaming
    protected readonly Logger Logger = LogManager.GetLogger(typeof(BaseSyncingHandler<,,>).FullName + ":" + typeof(TClusterResource).Name);

    private readonly IStateContainer _state;
    private readonly OperatorOptions _operatorOptions;
    private readonly IResourceComparer _comparer;
    private readonly IKubernetesClient _kubernetesClient;
    private readonly ClusterDefaults _clusterDefaults;
    private readonly IReactionHelper _reactionHelper;

    protected abstract string EntityName { get; }

    protected BaseSyncingHandler(IStateContainer state,
                                 OperatorOptions operatorOptions,
                                 IResourceComparer comparer,
                                 IKubernetesClient kubernetesClient,
                                 ClusterDefaults clusterDefaults,
                                 IReactionHelper reactionHelper)
    {
        _state = state;
        _operatorOptions = operatorOptions;
        _comparer = comparer;
        _kubernetesClient = kubernetesClient;
        _clusterDefaults = clusterDefaults;
        _reactionHelper = reactionHelper;
    }

    public async Task Handle(DeferredStateModified notification, CancellationToken cancellationToken)
    {
        if (!await _reactionHelper.CanReact(cancellationToken))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
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

        Logger.Trace($"Completed checking for entity generation after {stopwatch.ElapsedMilliseconds}ms.");
    }

    private async ValueTask CreateOrUpdate(ResourceIdentityPair<TClusterResource> clusterResource,
                                           string targetName,
                                           string targetNamespace,
                                           TTargetResource desiredResource,
                                           CancellationToken cancellationToken)
    {
        Logger.Info($"Out-dated {EntityName} '{targetNamespace}/{targetName}' entity detected, preparing to create/patch.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Create, but don't save the entity yet.
            var entity = await CreateTargetEntity(clusterResource, desiredResource, targetName, targetNamespace);
            if (entity == null)
            {
                Logger.Info($"Failed to update {EntityName} '{targetNamespace}/{targetName}' after {stopwatch.ElapsedMilliseconds}ms. Data disappeared.");
                return;
            }

            var annotations = _clusterDefaults.GetAnnotationsForManagedResources(clusterResource.Identity.Name, clusterResource.Identity.Namespace);
            foreach (var annotation in annotations)
            {
                entity.SetAnnotation(annotation.Key, annotation.Value);
            }

            await _kubernetesClient.Save(entity);

            // Only mark this entity as dirty after saving, in-case the object was never created.
            await _state.MarkAsDirty<TTargetResource>(targetName, targetNamespace, cancellationToken);
            Logger.Info($"Updated {EntityName} '{targetNamespace}/{targetName}' after {stopwatch.ElapsedMilliseconds}ms.");
        }
        catch (HttpOperationException e)
        {
            Logger.Warn(e, $"An error occurred. Response body: '{e.Response.Content}'.");
        }
        catch (Exception e)
        {
            Logger.Debug(e);
        }
    }

    private async ValueTask Delete(string targetName,
                                   string targetNamespace,
                                   CancellationToken cancellationToken)
    {
        Logger.Info($"Superfluous {EntityName} '{targetNamespace}/{targetName}' entity detected, preparing to delete.");
        await _state.MarkAsDirty<TTargetResource>(targetName, targetNamespace, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _kubernetesClient.Delete<TEntity>(targetName, targetNamespace);
            Logger.Info($"Deleted {EntityName} '{targetNamespace}/{targetName}' after {stopwatch.ElapsedMilliseconds}ms.");
        }
        catch (HttpOperationException e)
        {
            Logger.Warn(e, $"An error occurred. Response body: '{e.Response.Content}'.");
        }
        catch (Exception e)
        {
            Logger.Debug(e);
        }
    }

    private async ValueTask<IReadOnlyCollection<ResourceIdentityPair<TClusterResource>>> GetAvailableClusterResources(
        CancellationToken cancellationToken)
    {
        var resources = new List<ResourceIdentityPair<TClusterResource>>();
        foreach (var clusterResource in await _state.GetByType<TClusterResource>(cancellationToken))
        {
            if (string.Equals(clusterResource.Identity.Namespace, _operatorOptions.Namespace, StringComparison.OrdinalIgnoreCase))
            {
                resources.Add(clusterResource);
            }
        }

        return resources;
    }

    protected abstract ValueTask<ResourceIdentityPair<TClusterResource>?> GetBestBaseForNamespace(
        IEnumerable<ResourceIdentityPair<TClusterResource>> clusterResources,
        string @namespace);

    protected abstract ValueTask<TTargetResource?> CreateDesiredResource(ResourceIdentityPair<TClusterResource> baseResource,
                                                                         string targetName,
                                                                         string targetNamespace);

    protected abstract ValueTask<TEntity?> CreateTargetEntity(ResourceIdentityPair<TClusterResource> baseResource,
                                                              TTargetResource desiredResource,
                                                              string targetName,
                                                              string targetNamespace);

    protected abstract string GetTargetEntityName(string targetNamespace);
}
