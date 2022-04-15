using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Mutators
{
    [UsedImplicitly]
    public class DaemonSetHandler : IRequestHandler<EntityReconciled<V1DaemonSet>>, IRequestHandler<EntityDeleted<V1DaemonSet>>
    {
        public Task<Unit> Handle(EntityReconciled<V1DaemonSet> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }

        public Task<Unit> Handle(EntityDeleted<V1DaemonSet> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
