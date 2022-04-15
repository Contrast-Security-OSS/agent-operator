using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Mutators
{
    [UsedImplicitly]
    public class AgentConnectionHandler : IRequestHandler<EntityReconciled<V1Beta1AgentConnection>>, IRequestHandler<EntityDeleted<V1Beta1AgentConnection>>
    {
        public Task<Unit> Handle(EntityReconciled<V1Beta1AgentConnection> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }

        public Task<Unit> Handle(EntityDeleted<V1Beta1AgentConnection> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
