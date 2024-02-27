// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Counters;

public class PerformanceCountersListener : EventListener
{
    private readonly PerformanceCounterContainer _performanceCounterContainer;

    public PerformanceCountersListener(PerformanceCounterContainer performanceCounterContainer)
    {
        _performanceCounterContainer = performanceCounterContainer;
    }

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (!source.Name.Equals("System.Runtime"))
        {
            return;
        }

        EnableEvents(source, EventLevel.Verbose, EventKeywords.All, new Dictionary<string, string?>
        {
            ["EventCounterIntervalSec"] = "60"
        });
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventName == null
            || eventData.Payload == null
            || !eventData.EventName.Equals("EventCounters"))
        {
            return;
        }

        var counters = new Dictionary<string, decimal>();
        for (var i = 0; i < eventData.Payload.Count; ++i)
        {
            if (eventData.Payload[i] is IDictionary<string, object> eventPayload
                && TryGetRelevantMetric(eventPayload, out var counterDisplayName, out var counterMeanValue))
            {
                counters.TryAdd(counterDisplayName, counterMeanValue);
            }
        }

        _performanceCounterContainer.UpdateCounters(counters);
    }

    private static bool TryGetRelevantMetric(IDictionary<string, object> eventPayload,
                                             [NotNullWhen(true)] out string? counterDisplayName,
                                             out decimal counterMeanValue)
    {
        counterDisplayName = null;
        counterMeanValue = 0;

        if (eventPayload.TryGetValue("DisplayName", out var displayNameObj)
            && (eventPayload.TryGetValue("Mean", out var value)
                || eventPayload.TryGetValue("Increment", out value))
            && decimal.TryParse(value.ToString(), out var parsedValue))
        {
            counterDisplayName = displayNameObj.ToString();
            counterMeanValue = parsedValue;

            return counterDisplayName != null;
        }

        return false;
    }
}
