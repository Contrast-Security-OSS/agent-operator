// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Models
{
    public class TelemetryMeasurement
    {
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        public string Path { get; set; }

        public IReadOnlyDictionary<string, decimal> Values { get; set; } = new Dictionary<string, decimal>();

        public IReadOnlyDictionary<string, string> ExtraTags { get; set; } = new Dictionary<string, string>();

        public TelemetryMeasurement(string path)
        {
            Path = path;
        }
    }
}
