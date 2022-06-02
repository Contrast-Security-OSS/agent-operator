using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.OpenShift;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class DeploymentConfigApplier : BaseApplier<V1DeploymentConfig, DeploymentConfigResource>
    {
        public DeploymentConfigApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<DeploymentConfigResource> CreateFrom(V1DeploymentConfig entity, CancellationToken cancellationToken = default)
        {
            var resource = new DeploymentConfigResource(
                entity.Uid(),
                entity.Metadata.GetLabels(),
                entity.Spec.Template.GetPod(),
                entity.Spec.Selector.ToPodSelector()
            );

            return ValueTask.FromResult(resource);
        }
    }
}
