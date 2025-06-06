﻿// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Options;
using k8s;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Kube;

public interface IResourcePatcher
{
    ValueTask<bool> Patch<T>(string name, string? @namespace, Action<T> mutator) where T : class, IKubernetesObject<V1ObjectMeta>;
    ValueTask<bool> PatchStatus<T>(string name, string? @namespace, GenericCondition condition) where T : class, IKubernetesObject<V1ObjectMeta>;
}

public class ResourcePatcher : IResourcePatcher
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly JsonPatchDeltaFormatter _jsonFormatter;
    private readonly IKubernetesClient _client;
    private readonly KubernetesJsonSerializer _jsonSerializer;
    private readonly OperatorOptions _operatorOptions;

    public ResourcePatcher(JsonPatchDeltaFormatter jsonFormatter, IKubernetesClient client, KubernetesJsonSerializer jsonSerializer, OperatorOptions operatorOptions)
    {
        _jsonFormatter = jsonFormatter;
        _client = client;
        _jsonSerializer = jsonSerializer;
        _operatorOptions = operatorOptions;
    }

    public async ValueTask<bool> Patch<T>(string name, string? @namespace, Action<T> mutator) where T : class, IKubernetesObject<V1ObjectMeta>
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Value must not be empty or null.", nameof(name));
        }

        if (@namespace != null && string.IsNullOrEmpty(@namespace))
        {
            throw new ArgumentException("If set, value cannot be empty.", nameof(@namespace));
        }

        var entity = await _client.GetAsync<T>(name, @namespace);
        if (entity != null)
        {
            await Patch(entity, mutator);
        }
        else
        {
            Logger.Trace($"Could not locate entity '{entity.Namespace()}/{entity.Name()}' to patch.");
        }

        return entity != null;
    }

    private async ValueTask Patch<T>(T entity, Action<T> mutator) where T : IKubernetesObject<V1ObjectMeta>
    {
        var stopwatch = Stopwatch.StartNew();
        var entityCopy = _jsonSerializer.DeepClone(entity);

        var currentVersion = _jsonSerializer.ToJsonNode(entityCopy);
        mutator.Invoke(entityCopy);
        var nextVersion = _jsonSerializer.ToJsonNode(entityCopy);

        var diff = currentVersion.Diff(nextVersion, _jsonFormatter);
        if (diff != null && diff.AsArray().Any())
        {
            Logger.Trace(() => $"Preparing to patch '{entity.Namespace()}/{entity.Name()}' ('{entity.Kind}/{entity.ApiVersion}') "
                               + $"with '{diff.ToJsonString()}'.");

            try
            {
                await _client.Patch(entity, diff, _operatorOptions.FieldManagerName);
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                Logger.Trace("Entity disappeared while patching.");
            }

            Logger.Trace($"Patch complete after {stopwatch.ElapsedMilliseconds}ms.");
        }
    }

    public async ValueTask<bool> PatchStatus<T>(string name, string? @namespace, GenericCondition condition) where T : class, IKubernetesObject<V1ObjectMeta>
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Value must not be empty or null.", nameof(name));
        }

        if (@namespace != null && string.IsNullOrEmpty(@namespace))
        {
            throw new ArgumentException("If set, value cannot be empty.", nameof(@namespace));
        }

        var stopwatch = Stopwatch.StartNew();

        var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<T>();

        Logger.Trace(() =>
            $"Preparing to patch status '{@namespace}/{name}' ('{crd.Kind}/{crd.Version}') "
            + $"with '{KubernetesJson.Serialize(condition)}'.");

        try
        {
            await _client.PatchStatus<T>(name, @namespace, condition, _operatorOptions.FieldManagerName);
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.Trace("Entity disappeared while patching.");
        }

        Logger.Trace($"Patch complete after {stopwatch.ElapsedMilliseconds}ms.");

        return true;
    }
}
