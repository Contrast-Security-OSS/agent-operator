using System;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Injecting;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class PodApplier : BaseApplier<V1Pod, PodResource>
    {
        public PodApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<PodResource> CreateFrom(V1Pod entity, CancellationToken cancellationToken = default)
        {
            var isInjected = entity.GetAnnotation(InjectionConstants.IsInjectedAttributeName) is { } isInjectedAnnotation
                             && isInjectedAnnotation.Equals(true.ToString(), StringComparison.OrdinalIgnoreCase);

            var resource = new PodResource(
                entity.Metadata.GetLabels(),
                isInjected
            );

            return ValueTask.FromResult(resource);
        }
    }
}
