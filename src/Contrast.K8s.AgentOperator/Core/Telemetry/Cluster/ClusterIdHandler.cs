using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Cluster
{
    [UsedImplicitly]
    public class ClusterIdHandler : INotificationHandler<EntityReconciled<V1Secret>>, INotificationHandler<LeaderStateChanged>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IClusterIdWriter _clusterIdWriter;
        private readonly IClusterIdState _state;
        private readonly ITelemetryOptOut _optOut;
        private readonly TelemetryOptions _options;

        public ClusterIdHandler(IClusterIdWriter clusterIdWriter, IClusterIdState state, ITelemetryOptOut optOut, TelemetryOptions options)
        {
            _clusterIdWriter = clusterIdWriter;
            _state = state;
            _optOut = optOut;
            _options = options;
        }

        public Task Handle(EntityReconciled<V1Secret> notification, CancellationToken cancellationToken)
        {
            if (!_optOut.IsOptOutActive()
                && string.Equals(notification.Entity.Name(), _options.ClusterIdSecretName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(notification.Entity.Namespace(), _options.ClusterIdSecretNamespace, StringComparison.OrdinalIgnoreCase))
            {
                var clusterId = _clusterIdWriter.ParseClusterId(notification.Entity);
                if (clusterId != null)
                {
                    var updated = _state.SetClusterId(clusterId);
                    if (updated)
                    {
                        Logger.Trace($"Internal cluster id was updated. (Generated: {clusterId.CreatedOn:O})");
                    }
                }
            }

            return Task.CompletedTask;
        }

        public async Task Handle(LeaderStateChanged notification, CancellationToken cancellationToken)
        {
            if (!_optOut.IsOptOutActive()
                && notification.IsLeader)
            {
                var stopwatch = Stopwatch.StartNew();

                var clusterId = await _clusterIdWriter.GetId();
                if (clusterId == null)
                {
                    clusterId = ClusterId.NewId();
                    await _clusterIdWriter.SetId(clusterId);

                    Logger.Trace($"Cluster id was generated after {stopwatch.ElapsedMilliseconds}ms.");
                }
            }
        }
    }
}
