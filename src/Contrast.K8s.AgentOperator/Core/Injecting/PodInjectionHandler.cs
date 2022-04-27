using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    public class PodInjectionHandler : IRequestHandler<EntityCreating<V1Pod>, EntityCreatingMutationResult<V1Pod>>
    {
        public async Task<EntityCreatingMutationResult<V1Pod>> Handle(EntityCreating<V1Pod> request, CancellationToken cancellationToken)
        {
            request.Entity.SetLabel("contrast-injected", "false");
            return new NeedsChangeEntityCreatingMutationResult<V1Pod>(request.Entity);
        }
    }
}
