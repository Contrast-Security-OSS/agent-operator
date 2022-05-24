using Contrast.K8s.AgentOperator.Core.Telemetry.Models;
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
            var report = new ExceptionReport(loggerName, Layout.Render(logEvent), logEvent.Exception);

            TelemetryExceptionsBuffer.Instance.Add(report);
        }
    }
}
