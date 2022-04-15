using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class SecretApplier : IApplier<V1Secret>
    {
        public Task<Unit> Handle(EntityReconciled<V1Secret> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }

        public Task<Unit> Handle(EntityDeleted<V1Secret> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
