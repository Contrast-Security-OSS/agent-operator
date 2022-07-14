// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
            if (logEvent.Exception is { } exception)
            {
                var loggerName = logEvent.LoggerName ?? "<unknown>";
                if (ShouldReport(loggerName, exception))
                {
                    var report = new ExceptionReport(
                        GetShortLoggerName(loggerName),
                        Layout.Render(logEvent),
                        exception
                    );
                    TelemetryExceptionsBuffer.Instance.Add(report);
                }
            }
        }

        public static bool ShouldReport(string loggerName, Exception exception)
        {
            if (loggerName == "KubeOps.Operator.Kubernetes.ResourceWatcher")
            {
                // Ignore any of the common exceptions we don't care about.
                switch (exception)
                {
                    case HttpOperationException httpOperationException
                        when httpOperationException.Response.StatusCode is not HttpStatusCode.BadRequest:
                    case HttpRequestException { InnerException: IOException }:
                    case HttpRequestException { InnerException: SocketException { SocketErrorCode: SocketError.ConnectionRefused } }:
                    case IOException:
                        return false;
                }
            }

            return exception is not OperationCanceledException;
        }

        public static string GetShortLoggerName(string loggerName)
        {
            var split = loggerName.LastIndexOf('.');
            if (split > -1
                && split < loggerName.Length
                && loggerName[(split + 1)..] is { } shortName
                && !string.IsNullOrWhiteSpace(shortName))
            {
                return shortName.Trim();
            }

            return loggerName;
        }
    }
}
