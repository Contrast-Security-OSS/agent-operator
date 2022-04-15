using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Mutators
{
    [UsedImplicitly]
    public class StatefulSetHandler : IRequestHandler<EntityReconciled<V1StatefulSet>>, IRequestHandler<EntityDeleted<V1StatefulSet>>
    {
        public Task<Unit> Handle(EntityReconciled<V1StatefulSet> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }

        public Task<Unit> Handle(EntityDeleted<V1StatefulSet> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
