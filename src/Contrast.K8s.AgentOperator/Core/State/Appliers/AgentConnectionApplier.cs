using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class AgentConnectionApplier : BaseApplier<V1Beta1AgentConnection, AgentConnectionResource>
    {
        public AgentConnectionApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<AgentConnectionResource> CreateFrom(V1Beta1AgentConnection entity, CancellationToken cancellationToken = default)
        {
            var resource = new AgentConnectionResource();
            return ValueTask.FromResult(resource);
        }
    }
}
