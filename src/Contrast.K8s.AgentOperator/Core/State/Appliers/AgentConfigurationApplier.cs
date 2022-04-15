using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class AgentConfigurationApplier : BaseApplier<V1Beta1AgentConfiguration, AgentConfigurationResource>
    {
        public AgentConfigurationApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<AgentConfigurationResource> CreateFrom(V1Beta1AgentConfiguration entity, CancellationToken cancellationToken = default)
        {
            var resource = new AgentConfigurationResource();
            return ValueTask.FromResult(resource);
        }
    }
}
