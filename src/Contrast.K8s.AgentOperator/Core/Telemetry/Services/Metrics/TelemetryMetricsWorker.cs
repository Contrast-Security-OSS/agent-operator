// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using Contrast.K8s.AgentOperator.Core.Telemetry.Getters;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Services.Metrics;

[UsedImplicitly]
public class TelemetryMetricsWorker : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ITelemetryOptOut _optOut;
    private readonly IClusterIdState _clusterIdState;
    private readonly TelemetryService _telemetryService;
    private readonly StatusReportGenerator _statusReportGenerator;
    private readonly IsPublicTelemetryBuildGetter _isPublicTelemetryBuildGetter;

    public TelemetryMetricsWorker(ITelemetryOptOut optOut,
                                  IClusterIdState clusterIdState,
                                  TelemetryService telemetryService,
                                  StatusReportGenerator statusReportGenerator,
                                  IsPublicTelemetryBuildGetter isPublicTelemetryBuildGetter)
    {
        _optOut = optOut;
        _clusterIdState = clusterIdState;
        _telemetryService = telemetryService;
        _statusReportGenerator = statusReportGenerator;
        _isPublicTelemetryBuildGetter = isPublicTelemetryBuildGetter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isPublicTelemetryBuildGetter.IsPublicBuild())
        {
            Logger.Warn("This instance is not running a public build.");
        }

        if (_optOut.IsOptOutActive())
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        var clusterId = await _clusterIdState.GetClusterIdAsync(stoppingToken);
        if (DateTimeOffset.Now - clusterId.CreatedOn < TimeSpan.FromMinutes(1))
        {
            const string message = "The Contrast Agent Operator collects usage data in order to help us improve compatibility and security coverage. "
                                   + "The data is anonymous and does not contain application data. It is collected by Contrast and is never shared. "
                                   + "You can opt-out of telemetry by setting the CONTRAST_AGENT_TELEMETRY_OPTOUT environment variable to '1' or 'true'. "
                                   + "Read more about Contrast Agent Operator telemetry: https://docs.contrastsecurity.com/en/agent-operator-telemetry.html";

            Logger.Info(message);
        }

        // Wait for things to settle.
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var report = await _statusReportGenerator.Generate(stoppingToken);
                var result = await _telemetryService.SubmitMeasurement(report, stoppingToken);

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

            await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
        }
    }
}
