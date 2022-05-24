using System;

namespace Contrast.K8s.AgentOperator.Core.Telemetry
{
    public class TelemetryState
    {
        public string OperatorVersion { get; }

        public DateTimeOffset StartupTime { get; } = DateTimeOffset.Now;

        public TelemetryState(string operatorVersion)
        {
            OperatorVersion = operatorVersion;
        }
    }
}
