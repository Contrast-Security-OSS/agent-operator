// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using k8s.Models;
using KubeOps.KubernetesClient;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions;

public interface ISecretHelper
{
    ValueTask<string?> GetCachedSecretDataHashByRef(string name, string @namespace, string key);
    ValueTask<byte[]?> GetLiveSecretDataByRef(string name, string @namespace, string key);
}

public class SecretHelper : ISecretHelper
{
    private readonly IStateContainer _state;
    private readonly IKubernetesClient _kubernetesClient;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SecretHelper(IStateContainer state, IKubernetesClient kubernetesClient)
    {
        _state = state;
        _kubernetesClient = kubernetesClient;
    }

    public async ValueTask<string?> GetCachedSecretDataHashByRef(string name, string @namespace, string key)
    {
        var cachedSecret = await _state.GetById<SecretResource>(name, @namespace);
        if (cachedSecret?.KeyPairs != null)
        {
            if (cachedSecret.KeyPairs.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal)) is
                { DataHash: { } value })
            {
                return value;
            }

            Logger.Warn(
                $"Secret '{@namespace}/{name}' exists, but the key '{key}' did not exist. Available keys are [{string.Join(", ", cachedSecret.KeyPairs.Select(x => x.Key))}].");
        }
        else
        {
            Logger.Info(
                $"Secret '{@namespace}/{name}' does not exist, is not accessible, or contains no data. This error condition may be transitive.");
        }

        return null;
    }

    public async ValueTask<byte[]?> GetLiveSecretDataByRef(string name, string @namespace, string key)
    {
        var liveSecret = await _kubernetesClient.GetAsync<V1Secret>(name, @namespace);
        if (liveSecret?.Data != null)
        {
            if (liveSecret.Data.TryGetValue(key, out var value)
                && value != null)
            {
                return value;
            }

            Logger.Warn(
                $"Secret '{@namespace}/{name}' exists, but the key '{key}' no longer exists. Available keys are [{string.Join(", ", liveSecret.Data.Keys)}].");
        }
        else
        {
            Logger.Warn($"Secret '{@namespace}/{name}' no longer exists, is accessible, or contains data.");
        }

        return null;
    }
}
