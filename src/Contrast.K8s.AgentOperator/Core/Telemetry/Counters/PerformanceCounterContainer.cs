// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Counters
{
    public class PerformanceCounterContainer
    {
        private readonly ConcurrentDictionary<string, decimal> _counters = new();
        private readonly TaskCompletionSource<bool> _ready = new();

        public void UpdateCounters(IReadOnlyDictionary<string, decimal> counters)
        {
            foreach (var (key, value) in counters)
            {
                _counters.AddOrUpdate(key, value, (_, _) => value);
            }

            if (!_ready.Task.IsCompleted)
            {
                _ready.TrySetResult(true);
            }
        }

        public async Task<Dictionary<string, decimal>> GetCounters()
        {
            if (!_ready.Task.IsCompleted)
            {
                await _ready.Task;
            }

            return _counters.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
