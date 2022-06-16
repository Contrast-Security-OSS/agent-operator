// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Client
{
    public class SubmitMeasurementPayLoad
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("instance")]
        public string Instance { get; set; }

        [JsonProperty("tags")]
        public IReadOnlyDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        [JsonProperty("fields")]
        public IReadOnlyDictionary<string, decimal> Fields { get; set; } = new Dictionary<string, decimal>();

        public SubmitMeasurementPayLoad(DateTimeOffset timestamp, string instance)
        {
            Timestamp = timestamp;
            Instance = instance;
        }
    }
}
