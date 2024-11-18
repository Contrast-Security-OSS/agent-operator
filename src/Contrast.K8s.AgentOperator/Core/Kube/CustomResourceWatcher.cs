// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using k8s;
using KubeOps.Abstractions.Builder;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text.Json;
using Contrast.K8s.AgentOperator.Core.State;
using NLog;
using Contrast.K8s.AgentOperator.Core.Events;
using k8s.Autorest;

namespace Contrast.K8s.AgentOperator.Core.Kube;

// Custom implementation of the ResourceWatcher from KubeOps
// We are using the ResourceWatcher and not the LeadershipAwareResourceWatcher because
// we want the non-leaders to still have the cluster state in the event of a failover
//
// Changes:
// Use BackgroundService instead
// Remove entity cache, our state system will hold the cache
// Remove controllers, just enqueue the entity into the eventqueue for the state system to handle
// Quiet 404 responses (TODO figure out if we should just stop watching?)
public class CustomResourceWatcher<TEntity> : BackgroundService
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private uint _watcherReconnectRetries;

    private readonly IEventStream _eventStream;
    private readonly OperatorSettings _settings;
    private readonly IKubernetesClient _client;

    public CustomResourceWatcher(IEventStream eventStream,
        OperatorSettings settings,
        IKubernetesClient client)
    {
        _eventStream = eventStream;
        _settings = settings;
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? currentVersion = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach ((WatchEventType type, TEntity entity) in _client.WatchAsync<TEntity>(
                                   _settings.Namespace,
                                   resourceVersion: currentVersion,
                                   allowWatchBookmarks: true,
                                   cancellationToken: stoppingToken))
                {
                    Logger.Trace(() => 
                        $"Received watch event '{type}' for '{entity.Kind}/{entity.Name()}', last observed resource version: {entity.ResourceVersion()}.");

                    if (type == WatchEventType.Bookmark)
                    {
                        currentVersion = entity.ResourceVersion();
                        continue;
                    }

                    try
                    {
                        await OnEventAsync(type, entity, stoppingToken);
                    }
                    catch (KubernetesException e) when (e.Status.Code is (int)HttpStatusCode.GatewayTimeout)
                    {
                        Logger.Debug(e, "Watch restarting due to 504 Gateway Timeout.");
                        break;
                    }
                    catch (KubernetesException e) when (e.Status.Code is (int)HttpStatusCode.Gone)
                    {
                        // Special handling when our resource version is outdated.
                        throw;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Reconciliation of {type} for {entity.Kind}/{entity.Name()} failed.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Don't throw if the cancellation was indeed requested.
                break;
            }
            catch (HttpOperationException e) when (e.Response.StatusCode is HttpStatusCode.NotFound)
            {
                Logger.Debug($"Watcher for {typeof(TEntity).Name} delaying due to 404 Not Found.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (KubernetesException e) when (e.Status.Code is (int)HttpStatusCode.Gone)
            {
                Logger.Debug(e, "Watch restarting with reset bookmark due to 410 HTTP Gone.");
                currentVersion = null;
            }
            catch (Exception e)
            {
                await OnWatchErrorAsync(e, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            Logger.Debug($"Watcher for {typeof(TEntity).Name} was terminated and is reconnecting.");
        }
    }

    private async Task OnEventAsync(WatchEventType type, TEntity entity, CancellationToken cancellationToken)
    {
        switch (type)
        {
            case WatchEventType.Added:
            case WatchEventType.Modified:
                await ReconcileModificationAsync(entity, cancellationToken);
                break;
            case WatchEventType.Deleted:
                await ReconcileDeletionAsync(entity, cancellationToken);
                break;
            default:
                Logger.Warn($"Received unsupported event '{type}' for '{entity.Kind}/{entity.Name()}'.");
                break;
        }
    }

    private async Task OnWatchErrorAsync(Exception e, CancellationToken stoppingToken)
    {
        switch (e)
        {
            case SerializationException when
                e.InnerException is JsonException &&
                e.InnerException.Message.Contains("The input does not contain any JSON tokens"):
                Logger.Debug($"The watcher received an empty response for resource '{typeof(TEntity)}'");
                return;

            case HttpRequestException when
                e.InnerException is EndOfStreamException &&
                e.InnerException.Message.Contains("Attempted to read past the end of the stream."):
                Logger.Debug(
                    $"The watcher received a known error from the watched resource '{typeof(TEntity)}'. This indicates that there are no instances of this resource.");
                return;
        }

        Logger.Error(e, $"There was an error while watching the resource '{typeof(TEntity)}'.");

        _watcherReconnectRetries++;
        var delay = TimeSpan
            .FromSeconds(Math.Pow(2, Math.Clamp(_watcherReconnectRetries, 0, 5)))
            .Add(TimeSpan.FromMilliseconds(new Random().Next(0, 1000)));
        Logger.Warn(
            $"There were {_watcherReconnectRetries} errors / retries in the watcher. Waiting {delay.TotalSeconds}s before next attempt to connect.");
        await Task.Delay(delay, stoppingToken);
    }

    private async Task ReconcileModificationAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await _eventStream.DispatchDeferred(new EntityReconciled<TEntity>(entity), cancellationToken);
    }

    private async Task ReconcileDeletionAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await _eventStream.DispatchDeferred(new EntityDeleted<TEntity>(entity), cancellationToken);
    }
}
