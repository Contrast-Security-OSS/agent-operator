// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using k8s.Autorest;
using MediatR;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.State
{
    [UsedImplicitly]
    public class EventStreamWorker : BackgroundService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IEventStream _eventStream;
        private readonly IMediator _mediator;

        public EventStreamWorker(IEventStream eventStream, IMediator mediator)
        {
            _eventStream = eventStream;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var next = await _eventStream.DequeueNext(stoppingToken);

                    await _mediator.Publish(next, stoppingToken);
                }
                catch (HttpOperationException e)
                {
                    Logger.Warn(e, $"An error occurred. Response body: '{e.Response.Content}'.");
                }
                catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }
        }
    }
}
