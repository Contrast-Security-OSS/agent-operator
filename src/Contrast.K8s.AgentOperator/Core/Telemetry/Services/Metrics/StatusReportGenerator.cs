// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Services.Metrics
{
    public class StatusReportGenerator
    {
        private readonly TelemetryState _telemetryState;
        private readonly IStateContainer _clusterState;

        public StatusReportGenerator(TelemetryState telemetryState, IStateContainer clusterState)
        {
            _telemetryState = telemetryState;
            _clusterState = clusterState;
        }

        public async Task<TelemetryMeasurement> Generate(CancellationToken cancellationToken = default)
        {
            var uptimeSeconds = (decimal)(DateTimeOffset.Now - _telemetryState.StartupTime).TotalSeconds;

            var values = new Dictionary<string, decimal>
            {
                { "UptimeSeconds", uptimeSeconds }
            };

            var resourceStatistics = await GetResourceStatistics(cancellationToken);
            foreach (var (key, value) in resourceStatistics)
            {
                values.Add(key, value);
            }

            return new TelemetryMeasurement("status-report")
            {
                Values = values
            };
        }

        private async Task<Dictionary<string, decimal>> GetResourceStatistics(CancellationToken cancellationToken)
        {
            var metrics = new Dictionary<string, decimal>();

            var keys = await _clusterState.GetAllKeys(cancellationToken);

            foreach (var g in keys.GroupBy(x => x.Type))
            {
                var resourceName = g.Key.Name;

                var namespacesCount = g.Select(x => x.Namespace).Distinct().Count();
                metrics.Add($"Resources.{resourceName}.NamespacesCount", namespacesCount);

                var resourcesCount = g.Select(x => new { x.Name, x.Namespace }).Distinct().Count();
                metrics.Add($"Resources.{resourceName}.ResourcesCount", resourcesCount);
            }

            {
                var namespacesCount = keys.Select(x => x.Namespace).Distinct().Count();
                metrics.Add("Resources.Global.NamespacesCount", namespacesCount);

                var resourcesCount = keys.Select(x => new { x.Name, x.Namespace }).Distinct().Count();
                metrics.Add("Resources.Global.ResourcesCount", resourcesCount);
            }

            return metrics;
        }
    }
}
