using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class AgentInjectorApplier : BaseApplier<V1Beta1AgentInjector, AgentInjectorResource>
    {
        public AgentInjectorApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<AgentInjectorResource> CreateFrom(V1Beta1AgentInjector entity, CancellationToken cancellationToken = default)
        {
            var resource = new AgentInjectorResource();
            return ValueTask.FromResult(resource);
        }
    }
}
