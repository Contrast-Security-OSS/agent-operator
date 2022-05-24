using System;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Models
{
    public class ExceptionReport
    {
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

        public string LoggerName { get; init; }

        public string LogMessage { get; init; }

        public Exception Exception { get; init; }

        public ExceptionReport(string loggerName,
                               string logMessage,
                               Exception exception)
        {
            LoggerName = loggerName;
            LogMessage = logMessage;
            Exception = exception;
        }
    }
}
