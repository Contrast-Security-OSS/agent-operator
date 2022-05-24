using System.Collections.Generic;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry.Getters;

#nullable enable

namespace Contrast.K8s.AgentOperator.Core.Telemetry
{
    public class DefaultTagsFactory
    {
        private readonly IsPublicTelemetryBuildGetter _isPublicTelemetryBuildGetter;
        private readonly TelemetryState _telemetryState;
        private readonly MachineIdGetter _machineIdGetter;
        private readonly K8sClusterGetter _cluster;

        public DefaultTagsFactory(IsPublicTelemetryBuildGetter isPublicTelemetryBuildGetter,
                                  TelemetryState telemetryState,
                                  MachineIdGetter machineIdGetter,
                                  K8sClusterGetter cluster)
        {
            _isPublicTelemetryBuildGetter = isPublicTelemetryBuildGetter;
            _telemetryState = telemetryState;
            _machineIdGetter = machineIdGetter;
            _cluster = cluster;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetDefaultTags()
        {
            var defaultTags = new Dictionary<string, string>
            {
                { "Operator.IsPublicBuild", _isPublicTelemetryBuildGetter.IsPublicBuild().ToString() },
                { "Operator.Version", _telemetryState.OperatorVersion }
            };

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
}
