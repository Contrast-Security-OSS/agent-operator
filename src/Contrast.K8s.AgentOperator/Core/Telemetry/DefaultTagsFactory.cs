// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Leading;
using Contrast.K8s.AgentOperator.Core.Telemetry.Getters;
using Contrast.K8s.AgentOperator.Options;

namespace Contrast.K8s.AgentOperator.Core.Telemetry;

public class DefaultTagsFactory
{
    private readonly IsPublicTelemetryBuildGetter _isPublicTelemetryBuildGetter;
    private readonly TelemetryState _telemetryState;
    private readonly TelemetryOptions _telemetryOptions;
    private readonly MachineIdGetter _machineIdGetter;
    private readonly K8sClusterGetter _cluster;
    private readonly ILeaderElectionState _leaderElectionState;

    public DefaultTagsFactory(IsPublicTelemetryBuildGetter isPublicTelemetryBuildGetter,
                              TelemetryState telemetryState,
                              TelemetryOptions telemetryOptions,
                              MachineIdGetter machineIdGetter,
                              K8sClusterGetter cluster,
                              ILeaderElectionState leaderElectionState)
    {
        _isPublicTelemetryBuildGetter = isPublicTelemetryBuildGetter;
        _telemetryState = telemetryState;
        _telemetryOptions = telemetryOptions;
        _machineIdGetter = machineIdGetter;
        _cluster = cluster;
        _leaderElectionState = leaderElectionState;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetDefaultTags()
    {
        var defaultTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Operator.IsPublicBuild", _isPublicTelemetryBuildGetter.IsPublicBuild().ToString() },
            { "Operator.Version", _telemetryState.OperatorVersion },
            { "Operator.IsLeader", _leaderElectionState.IsLeader().ToString() }
        };

        if (!string.IsNullOrWhiteSpace(_telemetryOptions.InstallSource))
        {
            defaultTags.Add("Operator.InstallSource", _telemetryOptions.InstallSource);
        }

        if (await _cluster.GetClusterInfo() is { } clusterInfo)
        {
            defaultTags.Add("Cluster.BuildDate", clusterInfo.BuildDate);
            defaultTags.Add("Cluster.Compiler", clusterInfo.Compiler);
            defaultTags.Add("Cluster.GitCommit", clusterInfo.GitCommit);
            defaultTags.Add("Cluster.GitTreeState", clusterInfo.GitTreeState);
            defaultTags.Add("Cluster.GitVersion", clusterInfo.GitVersion);
            defaultTags.Add("Cluster.GoVersion", clusterInfo.GoVersion);
            defaultTags.Add("Cluster.Major", clusterInfo.Major);
            defaultTags.Add("Cluster.Minor", clusterInfo.Minor);
            defaultTags.Add("Cluster.Platform", clusterInfo.Platform);
        }

        return defaultTags;
    }

    public string GetMachineId()
    {
        return _machineIdGetter.GetMachineId();
    }
}
