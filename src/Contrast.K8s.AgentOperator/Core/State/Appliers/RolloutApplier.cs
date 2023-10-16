// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities.Argo;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class RolloutApplier : BaseApplier<V1Alpha1Rollout, RolloutResource>
    {
        public RolloutApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        public override ValueTask<RolloutResource> CreateFrom(V1Alpha1Rollout entity, CancellationToken cancellationToken = default)
        {
            var resource = new RolloutResource(
                entity.Uid(),
                entity.Metadata.GetLabels(),
                entity.Spec.Template.GetPod(),
                entity.Spec.Selector.ToPodSelector()
            );

            return ValueTask.FromResult(resource);
        }
    }
}
