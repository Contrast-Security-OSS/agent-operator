// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Base;

/// <summary>
/// Base for syncing cluster resources to namespaces
/// </summary>
/// <typeparam name="TClusterResource"></typeparam>
/// <typeparam name="TTargetResource"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public abstract class BaseSyncingHandler<TClusterResource, TTargetResource, TEntity>
    : INotificationHandler<DeferredStateModified>
    where TClusterResource : class, INamespacedResource
    where TTargetResource : class, INamespacedResource, IMutableResource
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    // ReSharper disable once InconsistentNaming
    protected readonly Logger Logger =
        LogManager.GetLogger(typeof(BaseSyncingHandler<,,>).FullName + ":" + typeof(TClusterResource).Name);

    private readonly IStateContainer _state;
    private readonly OperatorOptions _operatorOptions;
    private readonly IKubernetesClient _kubernetesClient;
    private readonly IReactionHelper _reactionHelper;

    protected abstract string EntityName { get; }

    protected BaseSyncingHandler(IStateContainer state,
        OperatorOptions operatorOptions,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper)
    {
        _state = state;
        _operatorOptions = operatorOptions;
        _kubernetesClient = kubernetesClient;
        _reactionHelper = reactionHelper;
    }

    public async Task Handle(DeferredStateModified notification, CancellationToken cancellationToken)
    {
        if (!await _reactionHelper.CanReact(cancellationToken))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        await Sync(cancellationToken);
        Logger.Trace($"Completed checking for entity generation after {stopwatch.ElapsedMilliseconds}ms.");
    }

    protected async ValueTask CreateOrUpdate(ResourceIdentityPair<TClusterResource> clusterResource,
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

            var annotations = ResourceAnnotations.GetAnnotationsForManagedResources(clusterResource.Identity.Name, clusterResource.Identity.Namespace);
            foreach (var annotation in annotations)
            {
                entity.SetAnnotation(annotation.Key, annotation.Value);
            }

            await _kubernetesClient.SaveAsync(entity, cancellationToken);

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

    protected async ValueTask Delete(string targetName,
        string targetNamespace,
        CancellationToken cancellationToken)
    {
        Logger.Info($"Superfluous {EntityName} '{targetNamespace}/{targetName}' entity detected, preparing to delete.");
        await _state.MarkAsDirty<TTargetResource>(targetName, targetNamespace, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _kubernetesClient.DeleteAsync<TEntity>(targetName, targetNamespace, cancellationToken);
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

    protected async ValueTask<IReadOnlyCollection<ResourceIdentityPair<TClusterResource>>> GetAvailableClusterResources(
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

    protected abstract ValueTask Sync(CancellationToken cancellationToken);

    protected abstract ValueTask<TEntity?> CreateTargetEntity(ResourceIdentityPair<TClusterResource> baseResource,
        TTargetResource desiredResource,
        string targetName,
        string targetNamespace);

    protected abstract ValueTask<TTargetResource?> CreateDesiredResource(
        TTargetResource? existingResource,
        ResourceIdentityPair<TClusterResource> baseResource,
        string targetName,
        string targetNamespace);
}
