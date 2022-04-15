using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Mutators
{
    [UsedImplicitly]
    public class AgentConfigurationHandler : IRequestHandler<EntityReconciled<V1Beta1AgentConfiguration>>, IRequestHandler<EntityDeleted<V1Beta1AgentConfiguration>>
    {
        public Task<Unit> Handle(EntityReconciled<V1Beta1AgentConfiguration> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }

        public Task<Unit> Handle(EntityDeleted<V1Beta1AgentConfiguration> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
