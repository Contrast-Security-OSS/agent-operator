// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Merging
{
    public class MergingStateModifiedHandler : INotificationHandler<StateModified>, INotificationHandler<TickChanged>
    {
        // This class basically implements head de-bouncing
        //   to merge many StateModified events into a single DeferredStateModified events.

        private readonly IMediator _mediator;
        private readonly MergingStateProvider _provider;

        public MergingStateModifiedHandler(IMediator mediator, MergingStateProvider provider)
        {
            _mediator = mediator;
            _provider = provider;
        }

        public Task Handle(StateModified notification, CancellationToken cancellationToken)
        {
            return Handle(false, cancellationToken);
        }

        public Task Handle(TickChanged notification, CancellationToken cancellationToken)
        {
            return Handle(true, cancellationToken);
        }

        private async Task Handle(bool isTick, CancellationToken cancellationToken = default)
        {
            if (await _provider.GetNextEvent(isTick, cancellationToken) is { } @event)
            {
                await _mediator.Publish(@event, cancellationToken);
            }
        }
    }
}
