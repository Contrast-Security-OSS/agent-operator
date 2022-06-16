// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry.Client;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;
using Contrast.K8s.AgentOperator.Core.Telemetry.Services.Exceptions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Telemetry
{
    public interface ITelemetryService
    {
        Task<TelemetrySubmissionResult> SubmitMeasurement(TelemetryMeasurement measurement,
                                                          CancellationToken cancellationToken = default);

        Task<TelemetrySubmissionResult> SubmitExceptionReports(IReadOnlyCollection<ExceptionReportWithOccurrences> exceptionReports,
                                                               CancellationToken cancellationToken = default);
    }

    [UsedImplicitly]
    public class TelemetryService : ITelemetryService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly DefaultTagsFactory _tagsFactory;
        private readonly ITelemetryClient _client;
        private readonly ExceptionReportMapper _exceptionReportMapper;

        public TelemetryService(DefaultTagsFactory tagsFactory,
                                ITelemetryClient client,
                                ExceptionReportMapper exceptionReportMapper)
        {
            _tagsFactory = tagsFactory;
            _client = client;
            _exceptionReportMapper = exceptionReportMapper;
        }

        public async Task<TelemetrySubmissionResult> SubmitMeasurement(TelemetryMeasurement measurement, CancellationToken cancellationToken = default)
        {
            var machineId = _tagsFactory.GetMachineId();
            var defaultTags = await _tagsFactory.GetDefaultTags();
            var tags = defaultTags
                       .Concat(measurement.ExtraTags)
                       .ToDictionary(x => x.Key, x => x.Value);
            var path = CreatePath(measurement.Path);

            var payload = new SubmitMeasurementPayLoad(measurement.Timestamp, machineId)
            {
                Fields = measurement.Values,
                Tags = tags
            };

#if DEBUG
            Logger.Debug(() => JsonConvert.SerializeObject(payload));
#endif

            try
            {
                using var result = await _client.SubmitMeasurement(
                    path,
                    new List<SubmitMeasurementPayLoad>
                    {
                        payload
                    },
                    cancellationToken
                );

                if (result.IsSuccessStatusCode)
                {
                    return TelemetrySubmissionResult.Success;
                }

                Logger.Trace($"An error occurred while submitting telemetry, got status code {result.StatusCode}.");

                if ((int)result.StatusCode >= 400
                    && (int)result.StatusCode < 500
                    && (int)result.StatusCode != 429)
                {
                    // Something is broken on our end.
                    return TelemetrySubmissionResult.PermanentError;
                }

                return TelemetrySubmissionResult.TransientError;
            }
            catch (Exception e)
            {
                Logger.Trace(e, "An error occurred while submitting telemetry.");
                return TelemetrySubmissionResult.PermanentError;
            }
        }

        public async Task<TelemetrySubmissionResult> SubmitExceptionReports(IReadOnlyCollection<ExceptionReportWithOccurrences> exceptionReports,
                                                                            CancellationToken cancellationToken = default)
        {
            var machineId = _tagsFactory.GetMachineId();
            var defaultTags = await _tagsFactory.GetDefaultTags();
            var path = CreatePath("exception-report");

            var submitReports = exceptionReports
                                .Select(er => _exceptionReportMapper.CreateFrom(er.Report, defaultTags, machineId, er.Occurrences))
                                .ToList();

#if DEBUG
            Logger.Debug(() => JsonConvert.SerializeObject(submitReports));
#endif

            try
            {
                using var result = await _client.SubmitExceptionReports(
                    path,
                    submitReports,
                    cancellationToken
                );

                if (result.IsSuccessStatusCode)
                {
                    return TelemetrySubmissionResult.Success;
                }

                Logger.Trace($"An error occurred while submitting exception reports, got status code {result.StatusCode}.");

                if ((int)result.StatusCode >= 400
                    && (int)result.StatusCode < 500
                    && (int)result.StatusCode != 429)
                {
                    // Something is broken on our end.
                    return TelemetrySubmissionResult.PermanentError;
                }

                return TelemetrySubmissionResult.TransientError;
            }
            catch (Exception e)
            {
                Logger.Trace(e, "An error occurred while submitting exception report.");
                return TelemetrySubmissionResult.PermanentError;
            }
        }

        private string CreatePath(string pathSuffix)
        {
            return $"agent-operator/{pathSuffix}";
        }
    }
}
