// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Services.Exceptions;

[UsedImplicitly]
public class TelemetryExceptionWorker : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly TelemetryOptions _options;
    private readonly TelemetryService _telemetryService;
    private readonly IClusterIdState _clusterIdState;

    public TelemetryExceptionWorker(TelemetryOptions options, TelemetryService telemetryService, IClusterIdState clusterIdState)
    {
        _options = options;
        _telemetryService = telemetryService;
        _clusterIdState = clusterIdState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.OptOut)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        // Don't submit telemetry until we have a cluster id.
        await _clusterIdState.GetClusterIdAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var report = await TelemetryExceptionsBuffer.Instance.Take(stoppingToken);

                // Does not currently support de-dup.
                var reportWithOccurrences = new ExceptionReportWithOccurrences(report);
                reportWithOccurrences.IncrementOccurrences();

                var result = await _telemetryService.SubmitExceptionReports(new[]
                {
                    reportWithOccurrences
                }, stoppingToken);

                if (result == TelemetrySubmissionResult.PermanentError)
                {
                    Logger.Trace("Got a permanent error while sending telemetry, disabling future telemetry submittion.");
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Logger.Trace(e, "A failure occurred during telemetry submission.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
