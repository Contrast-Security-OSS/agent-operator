// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Options;
using Nito.AsyncEx;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Merging
{
    public class MergingStateProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly TimeSpan _mergeInterval;
        private readonly AsyncLock _lock = new();

        private MergeState? _state;

        public MergingStateProvider(OperatorOptions options)
        {
            _mergeInterval = TimeSpan.FromSeconds(options.EventQueueMergeWindowSeconds);
        }

        public async ValueTask<DeferredStateModified?> GetNextEvent(bool isTick, CancellationToken cancellationToken = default)
        {
            using (await _lock.LockAsync(cancellationToken))
            {
                if (_state == null)
                {
                    if (isTick)
                    {
                        return null;
                    }

                    var mergeUntil = DateTimeOffset.Now + _mergeInterval;
                    Logger.Trace($"Merging state modified events until '{mergeUntil}'.");

                    // Not merging, start merging...
                    _state = new MergeState(mergeUntil);
                    return DeferredStateModified.NothingMerged;
                }

                if (_state.MergeUntil > DateTimeOffset.Now)
                {
                    // Is merging.
                    if (!isTick)
                    {
                        _state.IncrementMerged();
                    }

                    return null;
                }

                // Merging expired.
                var state = _state;
                _state = null;
                if (state.Merged > 0)
                {
                    Logger.Trace($"Flushing state modified, {state.Merged} events were merged.");
                    return new DeferredStateModified(state.Merged);
                }

                return null;
            }
        }

        private class MergeState
        {
            public DateTimeOffset MergeUntil { get; }
            public int Merged { get; private set; }

            public MergeState(DateTimeOffset mergeUntil)
            {
                MergeUntil = mergeUntil;
            }

            public void IncrementMerged()
            {
                Merged++;
            }
        }
    }
}
