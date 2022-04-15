using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class DeploymentApplier : BaseApplier<V1Deployment, DeploymentResource>
    {
        public DeploymentApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<DeploymentResource> CreateFrom(V1Deployment entity, CancellationToken cancellationToken = default)
        {
            var resource = new DeploymentResource(
                entity.Metadata.GetLabels()
            );

            return ValueTask.FromResult(resource);
        }
    }
}
