// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;
using k8s.Autorest;
using NLog;
using NLog.Targets;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Services.Exceptions
{
    [Target("TelemetryExceptions")]
    public class TelemetryExceptionsTarget : TargetWithLayout
    {
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent.Exception == null)
            {
                return;
            }

            var loggerName = logEvent.LoggerName ?? "<unknown>";
            if (loggerName == "KubeOps.Operator.Kubernetes.ResourceWatcher")
            {
                // Ignore any of the common exceptions we don't care about.

                if (logEvent.Exception is HttpOperationException httpOperationException
                    && httpOperationException.Response.StatusCode is HttpStatusCode.NotFound)
                {
                    return;
                }

                if (logEvent.Exception is HttpRequestException { InnerException: EndOfStreamException })
                {
                    return;
                }
            }

            if (logEvent.Exception is TaskCanceledException)
            {
                return;
            }

            var report = new ExceptionReport(loggerName, Layout.Render(logEvent), logEvent.Exception);
            TelemetryExceptionsBuffer.Instance.Add(report);
        }
    }
}
