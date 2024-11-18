// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Options;
using k8s;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;

public interface IClusterIdWriter
{
    Task<ClusterId?> GetId();
    Task SetId(ClusterId clusterId);
    ClusterId? ParseClusterId(V1Secret? clusterIdSecret);
}

public class ClusterIdWriter : IClusterIdWriter
{
    private const string PayloadKey = "payload";

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IKubernetesClient _client;
    private readonly TelemetryOptions _options;

    public ClusterIdWriter(IKubernetesClient client, TelemetryOptions options)
    {
        _client = client;
        _options = options;
    }

    public async Task<ClusterId?> GetId()
    {
        try
        {
            var clusterIdSecret = await _client.GetAsync<V1Secret>(_options.ClusterIdSecretName, _options.ClusterIdSecretNamespace);

            return ParseClusterId(clusterIdSecret);
        }
        catch (HttpOperationException e)
        {
            Logger.Warn(e, e.Response.Content);
        }
        catch (Exception e)
        {
            Logger.Warn(e);
        }

        return null;
    }

    public ClusterId? ParseClusterId(V1Secret? clusterIdSecret)
    {
        try
        {
            if (clusterIdSecret?.Data is { } data
                && data.TryGetValue(PayloadKey, out var bytes))
            {
                var json = Encoding.UTF8.GetString(bytes);
                var clusterId = KubernetesJson.Deserialize<ClusterId>(json);

                if (clusterId is { }
                    && clusterId.Guid != default
                    && clusterId.CreatedOn != default)
                {
                    return clusterId;
                }
            }
        }
        catch (HttpOperationException e)
        {
            Logger.Warn(e, e.Response.Content);
        }
        catch (Exception e)
        {
            Logger.Warn(e);
        }

        return null;
    }

    public async Task SetId(ClusterId clusterId)
    {
        var json = KubernetesJson.Serialize(clusterId);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _client.SaveAsync(new V1Secret
        {
            Metadata = new V1ObjectMeta(
                name: _options.ClusterIdSecretName,
                namespaceProperty: _options.ClusterIdSecretNamespace
            ),
            Data = new Dictionary<string, byte[]>
            {
                { PayloadKey, bytes }
            }
        });
    }
}
