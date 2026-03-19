// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Models;
using NLog;
using System;

namespace Contrast.K8s.AgentOperator.Core.Kube;

public static class ClusterVersionHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Get and parse the cluster version, removing trailing (+1234) minor version information
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public static Version? GetClusterVersion(IKubernetes client)
    {
        try
        {
            var rawVersion = client.Version.GetCode();
            var clusterVersion = TryParseClusterVersion(rawVersion);

            if (clusterVersion != null)
            {
                Logger.Info($"Cluster version detected: {clusterVersion}");
                return clusterVersion;
            }
            else
            {
                Logger.Warn($"Unable to parse cluster version (Major: '{rawVersion.Major}', Minor: '{rawVersion.Minor}'). "
                            + "Skipping Kubernetes version requirement checks.");
            }
        }
        catch (Exception e)
        {
            Logger.Warn(e, "Failed to query the cluster version. Skipping Kubernetes version requirement checks.");
        }
        return null;
    }

    public static Version? TryParseClusterVersion(VersionInfo version)
    {
        if (string.IsNullOrWhiteSpace(version.Major) || string.IsNullOrWhiteSpace(version.Minor))
        {
            return null;
        }

        // Minor version can contain a trailing '+' (e.g. "35+") in some distributions.
        var normalizedMinor = version.Minor.TrimEnd('+');

        int parsedMajor, parsedMinor = 0;
        var parsed = int.TryParse(version.Major, out parsedMajor)
               && int.TryParse(normalizedMinor, out parsedMinor);
        
        return parsed ? new Version(parsedMajor, parsedMinor) : null;
    }

}
