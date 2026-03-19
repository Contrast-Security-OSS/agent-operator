// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Contrast.K8s.AgentOperator.Options;
using k8s;
using NLog;

namespace Contrast.K8s.AgentOperator.Core;

public static class ClusterVersionValidator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Inspects <see cref="OperatorOptions"/> for boolean properties annotated with
    /// <see cref="RequiresMinimumKubernetesVersionAttribute"/>.  Any option that is
    /// <c>true</c> but unsupported by the current cluster version will be set to
    /// <c>false</c> and a warning will be logged.
    /// </summary>
    public static OperatorOptions ValidateOptions(OperatorOptions options, IKubernetes client)
    {
        int clusterMajor;
        int clusterMinor;
        bool versionKnown;

        try
        {
            var version = client.Version.GetCode();

            versionKnown = TryParseClusterVersion(version.Major, version.Minor, out clusterMajor, out clusterMinor);

            if (versionKnown)
            {
                Logger.Info($"Cluster version detected: {clusterMajor}.{clusterMinor}");
            }
            else
            {
                Logger.Warn($"Unable to parse cluster version (Major: '{version.Major}', Minor: '{version.Minor}'). "
                            + "Skipping Kubernetes version requirement checks.");
            }
        }
        catch (Exception e)
        {
            Logger.Warn(e, "Failed to query the cluster version. Skipping Kubernetes version requirement checks.");
            return options;
        }

        foreach (var property in typeof(OperatorOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType != typeof(bool))
            {
                continue;
            }

            var attr = property.GetCustomAttribute<RequiresMinimumKubernetesVersionAttribute>();
            if (attr is null)
            {
                continue;
            }

            var isEnabled = (bool)property.GetValue(options)!;
            if (!isEnabled)
            {
                continue;
            }

            if (!versionKnown)
            {
                // Cannot verify the requirement – leave enabled and let the cluster reject it.
                continue;
            }

            if (clusterMajor > attr.Major || (clusterMajor == attr.Major && clusterMinor >= attr.Minor))
            {
                Logger.Info($"Cluster version {clusterMajor}.{clusterMinor} meets the requirement "
                            + $"for {property.Name} (>= {attr.Major}.{attr.Minor}).");
                continue;
            }

            Logger.Warn($"{property.Name} requires Kubernetes {attr.Major}.{attr.Minor}+, "
                        + $"but the cluster is running {clusterMajor}.{clusterMinor}. "
                        + "The option has been disabled.");

            // Records support 'with' only from the caller side; use reflection to create
            // a new instance with this property set to false.
            var constructor = typeof(OperatorOptions).GetConstructors()[0];
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = typeof(OperatorOptions)
                           .GetProperty(parameters[i].Name!, BindingFlags.Public | BindingFlags.Instance)?
                           .GetValue(options);
            }

            // Find the matching constructor parameter and override it.
            for (var i = 0; i < parameters.Length; i++)
            {
                if (string.Equals(parameters[i].Name, property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    args[i] = false;
                    break;
                }
            }

            options = (OperatorOptions)constructor.Invoke(args);
        }

        return options;
    }

    public static bool TryParseClusterVersion(string? major, string? minor, out int parsedMajor, out int parsedMinor)
    {
        parsedMajor = 0;
        parsedMinor = 0;

        if (string.IsNullOrWhiteSpace(major) || string.IsNullOrWhiteSpace(minor))
        {
            return false;
        }

        // Minor version can contain a trailing '+' (e.g. "35+") in some distributions.
        var normalizedMinor = minor.TrimEnd('+');

        return int.TryParse(major, out parsedMajor)
               && int.TryParse(normalizedMinor, out parsedMinor);
    }
}
