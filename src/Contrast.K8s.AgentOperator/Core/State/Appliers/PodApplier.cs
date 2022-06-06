using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting;
using Contrast.K8s.AgentOperator.Core.Reactions.Monitoring;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
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

        public override ValueTask<PodResource> CreateFrom(V1Pod entity, CancellationToken cancellationToken = default)
        {
            var isInjected = entity.GetAnnotation(InjectionConstants.IsInjectedAttributeName) is { } isInjectedAnnotation
                             && isInjectedAnnotation.Equals(true.ToString(), StringComparison.OrdinalIgnoreCase);

            var injectionStatus = entity.Status?.Conditions?.Where(x => x.Type == PodConditionConstants.InjectionConvergenceConditionType)
                                        .Select(x => new PodInjectionConvergenceCondition(x.Status, x.Reason, x.Message))
                                        .SingleOrDefault();

            var resource = new PodResource(
                entity.Metadata.GetLabels(),
                isInjected,
                injectionStatus
            );

            return ValueTask.FromResult(resource);
        }
    }
}
