using System;

namespace Contrast.AgentUpgrader.Service.Telemetry.Models
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
