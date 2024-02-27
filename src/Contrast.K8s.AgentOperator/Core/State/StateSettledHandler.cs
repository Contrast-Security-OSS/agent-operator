// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State;

public class StateSettledHandler : INotificationHandler<StateSettled>
{
    private readonly IMediator _mediator;
    private readonly IStateContainer _state;

    public StateSettledHandler(IMediator mediator, IStateContainer state)
    {
        _mediator = mediator;
        _state = state;
    }

    public async Task Handle(StateSettled notification, CancellationToken cancellationToken)
    {
        await _state.Settled(cancellationToken);
        await _mediator.Publish(new StateModified(), cancellationToken);
    }
}
