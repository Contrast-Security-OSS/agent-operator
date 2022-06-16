// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
