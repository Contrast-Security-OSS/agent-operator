using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Mutators
{
    [UsedImplicitly]
    public class DeploymentHandler : IRequestHandler<EntityReconciled<V1Deployment>>, IRequestHandler<EntityDeleted<V1Deployment>>
    {
        private readonly IStateContainer _stateContainer;

        public DeploymentHandler(IStateContainer stateContainer)
        {
            _stateContainer = stateContainer;
        }

        public async Task<Unit> Handle(EntityDeleted<V1Deployment> request, CancellationToken cancellationToken)
        {
            var entity = request.Entity;

            var resource = new DeploymentResource(
                entity.Metadata.GetLabels()
            );

            await _stateContainer.AddOrReplaceById(entity.Name(), entity.Namespace(), resource, cancellationToken);

            return Unit.Value;
        }

        public async Task<Unit> Handle(EntityReconciled<V1Deployment> request, CancellationToken cancellationToken)
        {
            var entity = request.Entity;
            await _stateContainer.RemoveById<DeploymentResource>(entity.Name(), entity.Namespace(), cancellationToken);

            return Unit.Value;
        }
    }
}
