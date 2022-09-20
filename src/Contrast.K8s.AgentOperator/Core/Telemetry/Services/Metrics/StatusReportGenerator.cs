// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.Telemetry.Counters;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Services.Metrics
{
    public class StatusReportGenerator
    {
        private readonly TelemetryState _telemetryState;
        private readonly IStateContainer _clusterState;
        private readonly PerformanceCounterContainer _performanceCounterContainer;

        public StatusReportGenerator(TelemetryState telemetryState, IStateContainer clusterState, PerformanceCounterContainer performanceCounterContainer)
        {
            _telemetryState = telemetryState;
            _clusterState = clusterState;
            _performanceCounterContainer = performanceCounterContainer;
        }

        public async Task<TelemetryMeasurement> Generate(CancellationToken cancellationToken = default)
        {
            var uptimeSeconds = (decimal)(DateTimeOffset.Now - _telemetryState.StartupTime).TotalSeconds;

            var values = new Dictionary<string, decimal>
            {
                { "UptimeSeconds", uptimeSeconds }
            };

            foreach (var (key, value) in await GetResourceStatistics(cancellationToken))
            {
                values.Add(key, value);
            }

            foreach (var (key, value) in await GetInjectionStatistics(cancellationToken))
            {
                values.Add(key, value);
            }

            foreach (var (key, value) in await GetPerformanceStatistics())
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

        private async Task<Dictionary<string, decimal>> GetInjectionStatistics(CancellationToken cancellationToken)
        {
            var metrics = new Dictionary<string, decimal>();

            var pods = await _clusterState.GetByType<PodResource>(cancellationToken);
            foreach (var g in pods.GroupBy(x => x.Resource.InjectionType))
            {
                metrics.Add($"Injected.{g.Key}.PodsCount", g.Count());
            }

            var podsInjectedCount = pods.Count(x => x.Resource.IsInjected);
            metrics.Add("Injected.PodsCount", podsInjectedCount);

            return metrics;
        }

        private async Task<Dictionary<string, decimal>> GetPerformanceStatistics()
        {
            var metrics = new Dictionary<string, decimal>();

            var counters = await _performanceCounterContainer.GetCounters();
            foreach (var (key, value) in counters)
            {
                var normalizedKey = key.Replace(" ", "")
                                       .Replace("(", "")
                                       .Replace(")", "")
                                       .Replace("%", "Percent");
                metrics.Add($"Performance.{normalizedKey}", value);
            }

            return metrics;
        }
    }
}
