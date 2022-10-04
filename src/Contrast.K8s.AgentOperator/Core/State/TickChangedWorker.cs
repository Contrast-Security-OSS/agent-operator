// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Microsoft.Extensions.Hosting;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public class TickChangedWorker : BackgroundService
    {
        private readonly IEventStream _eventStream;

        public TickChangedWorker(IEventStream eventStream)
        {
            _eventStream = eventStream;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tickDelay = TimeSpan.FromSeconds(1);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(tickDelay, stoppingToken);
                await _eventStream.DispatchDeferred(TickChanged.Instance, stoppingToken);
            }
        }
    }
}
