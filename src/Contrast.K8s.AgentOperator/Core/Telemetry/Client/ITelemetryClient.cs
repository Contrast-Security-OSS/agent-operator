// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Client
{
    public interface ITelemetryClient
    {
        [Header("User-Agent")]
        string UserAgent { get; set; }

        [Post("api/v1/telemetry/metrics/{path}"), AllowAnyStatusCode]
        Task<HttpResponseMessage> SubmitMeasurement([Path(UrlEncode = false)] string path,
                                                    [Body] IReadOnlyCollection<SubmitMeasurementPayLoad> measurements,
                                                    CancellationToken cancellationToken = default);

        [Post("api/v1/telemetry/exceptions/{path}"), AllowAnyStatusCode]
        Task<HttpResponseMessage> SubmitExceptionReports([Path(UrlEncode = false)] string path,
                                                         [Body] IReadOnlyCollection<SubmitExceptionReportPayload> reports,
                                                         CancellationToken cancellationToken = default);
    }
}
