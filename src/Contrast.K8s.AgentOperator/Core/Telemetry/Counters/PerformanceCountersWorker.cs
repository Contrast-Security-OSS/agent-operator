// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Counters;

[UsedImplicitly]
public class PerformanceCountersWorker : BackgroundService
{
    private readonly Func<PerformanceCountersListener> _performanceCountersListenerFactory;

    public PerformanceCountersWorker(Func<PerformanceCountersListener> performanceCountersListenerFactory)
    {
        _performanceCountersListenerFactory = performanceCountersListenerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = _performanceCountersListenerFactory.Invoke();
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
