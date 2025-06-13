// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.Events;
using MediatR;
using System.Threading.Tasks;
using System.Threading;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using NLog;
using Contrast.K8s.AgentOperator.Core.State;
using KubeOps.KubernetesClient;
using k8s.Autorest;
using System.Diagnostics;
using System.Security.Cryptography;
using k8s.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using System.Text;
using Contrast.K8s.AgentOperator.Entities;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Secrets;

/// <summary>
/// Creates the file secret from the AgentConnectionSecret so we can mount as a volume secret if enabled
/// </summary>
public class AgentConnectionVolumeSecretHandler : INotificationHandler<DeferredStateModified>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IStateContainer _state;
    private readonly IKubernetesClient _kubernetesClient;
    private readonly IReactionHelper _reactionHelper;
    private readonly ISecretHelper _secretHelper;

    private string EntityName => "AgentConnectionVolumeSecret";
    private string ConfigFilename => "contrast_security.yaml";
    private string HashAnnotation => "agents.contrastsecurity.com/connection-secret-hash";

    public AgentConnectionVolumeSecretHandler(IStateContainer state,
        IKubernetesClient kubernetesClient,
        IReactionHelper reactionHelper,
        ISecretHelper secretHelper)
    {
        _state = state;
        _kubernetesClient = kubernetesClient;
        _reactionHelper = reactionHelper;
        _secretHelper = secretHelper;
    }

    public async Task Handle(DeferredStateModified notification, CancellationToken cancellationToken)
    {
        if (!await _reactionHelper.CanReact(cancellationToken))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var agentConnections = await _state.GetByType<AgentConnectionResource>(cancellationToken);
        foreach (var connection in agentConnections)
        {
            if (connection.Resource.MountAsVolume != true)
            {
                continue;
            }

            var resourceName = VolumeSecrets.GetConnectionVolumeSecretName(connection.Identity.Name);
            var resourceNamespace = connection.Identity.Namespace;

            if (await _state.GetIsDirty<SecretResource>(resourceName, resourceNamespace, cancellationToken))
            {
                Logger.Trace($"Ignoring dirty '{EntityName}' '{resourceName}/{resourceNamespace}'.");
                return;
            }

            var existingResource =
                await _state.GetByIdWithMetadata<SecretResource>(resourceName, resourceNamespace, cancellationToken);

            var derivedHash = await CreateDerivedSecretHash(connection);

            if (existingResource == null || existingResource.Metadata?.OperatorAnnotations.GetAnnotation(HashAnnotation) != derivedHash)
            {
                await CreateOrUpdate(connection, derivedHash, resourceName, resourceNamespace, cancellationToken);
            }
        }

        Logger.Trace($"Completed {EntityName} entity generation after {stopwatch.ElapsedMilliseconds}ms.");
    }

    private V1Secret CreateEntity(byte[] config, string name, string ns)
    {
        return new V1Secret(
            metadata: new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns
            },
            data: new Dictionary<string, byte[]>
            {
                { ConfigFilename, config }
            }
        );
    }

    private async Task<string> CreateDerivedSecretHash(ResourceIdentityPair<AgentConnectionResource> connectionResource)
    {
        var resource = connectionResource.Resource;
        var ns = connectionResource.Identity.Namespace;

        var secretKeyHashes = new StringBuilder();

        if (resource.Token != null)
        {
            var tokenHash = await _secretHelper.GetCachedSecretDataHashByRef(resource.Token.Name, ns, resource.Token.Key);
            if (tokenHash != null)
            {
                secretKeyHashes.Append(tokenHash);
            }
        }

        if (resource.UserName != null)
        {
            var usernameHash = await _secretHelper.GetCachedSecretDataHashByRef(resource.UserName.Name, ns, resource.UserName.Key);
            if (usernameHash != null)
            {
                secretKeyHashes.Append(usernameHash);
            }
        }

        if (resource.ApiKey != null)
        {
            var apiKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(resource.ApiKey.Name, ns, resource.ApiKey.Key);
            if (apiKeyHash != null)
            {
                secretKeyHashes.Append(apiKeyHash);
            }
        }

        if (resource.ServiceKey != null)
        {
            var serviceKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(resource.ServiceKey.Name, ns, resource.ServiceKey.Key);
            if (serviceKeyHash != null)
            {
                secretKeyHashes.Append(serviceKeyHash);
            }
        }

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(secretKeyHashes.ToString()));
        return HexConverter.ToLowerHex(bytes);
    }

    private async Task<byte[]?> CreateLiveConfig(ResourceIdentityPair<AgentConnectionResource> connectionResource)
    {
        var apiConfig = new Dictionary<string, string>();
        var resource = connectionResource.Resource;
        var ns = connectionResource.Identity.Namespace;

        if (resource.Token != null)
        {
            var tokenData = await _secretHelper.GetLiveSecretDataByRef(resource.Token.Name, ns, resource.Token.Key);
            if (tokenData != null)
            {
                apiConfig.Add("token", Encoding.UTF8.GetString(tokenData));
            }
        }

        if (resource.UserName != null)
        {
            var usernameData =
                await _secretHelper.GetLiveSecretDataByRef(resource.UserName.Name, ns, resource.UserName.Key);
            if (usernameData != null)
            {
                apiConfig.Add("user_name", Encoding.UTF8.GetString(usernameData));
            }
        }

        if (resource.ApiKey != null)
        {
            var apiKeyData = await _secretHelper.GetLiveSecretDataByRef(resource.ApiKey.Name, ns, resource.ApiKey.Key);
            if (apiKeyData != null)
            {
                apiConfig.Add("api_key", Encoding.UTF8.GetString(apiKeyData));
            }
        }

        if (resource.ServiceKey != null)
        {
            var serviceKeyData =
                await _secretHelper.GetLiveSecretDataByRef(resource.ServiceKey.Name, ns, resource.ServiceKey.Key);
            if (serviceKeyData != null)
            {
                apiConfig.Add("service_key", Encoding.UTF8.GetString(serviceKeyData));
            }
        }

        var config = new Dictionary<string, Dictionary<string, string>> { { "api", apiConfig } };

        try
        {
            var serializer = new Serializer();
            var yaml = serializer.Serialize(config);
            return Encoding.UTF8.GetBytes(yaml);
        }
        catch (Exception ex)
        {
            if (ex is YamlException yex)
            {
                // we don't want to push this exception to telemetry
                Logger.Warn(
                    $"Error serializing {EntityName} YAML at line:{yex.Start.Line}. Error: {ex.GetType().Name}: {ex.Message}");
            }
            else
            {
                Logger.Warn(ex, $"Error serializing {EntityName} YAML");
            }
        }

        return null;
    }

    private async Task CreateOrUpdate(ResourceIdentityPair<AgentConnectionResource> connection, string derivedHash, string resourceName, string ns,
        CancellationToken cancellationToken)
    {
        Logger.Info($"Out-dated {EntityName} '{ns}/{resourceName}' entity detected, preparing to create/patch.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var config = await CreateLiveConfig(connection);
            if (config == null)
            {
                Logger.Warn($"Failed to create {EntityName} config");
                return;
            }

            var liveConnection = await _kubernetesClient.GetAsync<V1Beta1AgentConnection>(connection.Identity.Name, ns, cancellationToken);
            if (liveConnection == null)
            {
                Logger.Info(
                    $"AgentConnection '{connection.Identity.Namespace}/{connection.Identity.Name}' does not exist, or is not accessible. This error condition may be transitive.");
                return;
            }

            // Create, but don't save the entity yet.
            var entity = CreateEntity(config, resourceName, ns);

            var annotations = ResourceAnnotations.GetAnnotationsForManagedResources(connection.Identity.Name, ns);
            foreach (var annotation in annotations)
            {
                entity.SetAnnotation(annotation.Key, annotation.Value);
            }

            entity.SetAnnotation(HashAnnotation, derivedHash);

            // Let k8s do the cleanup when the AgentConnection is deleted
            entity.Metadata.OwnerReferences = new List<V1OwnerReference>
            {
                new()
                {
                    Name = liveConnection.Name(),
                    ApiVersion = liveConnection.ApiVersion,
                    Kind = liveConnection.Kind,
                    Uid = liveConnection.Uid()
                }
            };

            await _kubernetesClient.SaveAsync(entity, cancellationToken);

            // Only mark this entity as dirty after saving, in-case the object was never created.
            await _state.MarkAsDirty<SecretResource>(resourceName, ns, cancellationToken);
            Logger.Info($"Updated {EntityName} '{ns}/{resourceName}' after {stopwatch.ElapsedMilliseconds}ms.");
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
}
