// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;

public interface IClusterIdState
{
    ClusterId? GetClusterId();
    bool SetClusterId(ClusterId clusterId);
    Task<ClusterId> GetClusterIdAsync(CancellationToken cancellationToken = default);
}

public class ClusterIdState : IClusterIdState
{
    private ClusterId? _cache;

    public ClusterId? GetClusterId()
    {
        return _cache;
    }

    public bool SetClusterId(ClusterId clusterId)
    {
        var lastId = _cache;
        _cache = clusterId;

        var updated = lastId != _cache;
        return updated;
    }

    public async Task<ClusterId> GetClusterIdAsync(CancellationToken cancellationToken = default)
    {
        ClusterId? clusterId;
        while ((clusterId = GetClusterId()) == null)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        return clusterId;
    }
}
