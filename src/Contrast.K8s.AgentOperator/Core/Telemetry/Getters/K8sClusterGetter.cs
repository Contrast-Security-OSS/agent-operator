// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using k8s;
using KubeOps.KubernetesClient;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Getters;

// ReSharper disable once InconsistentNaming
public class K8sClusterGetter
{
    private readonly IKubernetesClient _kubernetesClient;
    private ClusterInfo? _cache;

    public K8sClusterGetter(IKubernetesClient kubernetesClient)
    {
        _kubernetesClient = kubernetesClient;
    }

    public async Task<ClusterInfo?> GetClusterInfo()
    {
        return _cache ??= await GetClusterInfoImpl();
    }

    private async Task<ClusterInfo?> GetClusterInfoImpl()
    {
        try
        {
            var result = await _kubernetesClient.ApiClient.Version.GetCodeAsync();
            return new ClusterInfo(
                result.BuildDate ?? "<unknown>",
                result.Compiler ?? "<unknown>",
                result.GitCommit ?? "<unknown>",
                result.GitTreeState ?? "<unknown>",
                result.GitVersion ?? "<unknown>",
                result.GoVersion ?? "<unknown>",
                result.Major ?? "<unknown>",
                result.Minor ?? "<unknown>",
                result.Platform ?? "<unknown>"
            );
        }
        catch
        {
            return null;
        }
    }

    public record ClusterInfo(string BuildDate,
                              string Compiler,
                              string GitCommit,
                              string GitTreeState,
                              string GitVersion,
                              string GoVersion,
                              string Major,
                              string Minor,
                              string Platform);
}
