using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class StatefulSetApplier : BaseApplier<V1StatefulSet, StatefulSetResource>
    {
        public StatefulSetApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<StatefulSetResource> CreateFrom(V1StatefulSet entity, CancellationToken cancellationToken = default)
        {
            var resource = new StatefulSetResource();
            return ValueTask.FromResult(resource);
        }
    }
}
