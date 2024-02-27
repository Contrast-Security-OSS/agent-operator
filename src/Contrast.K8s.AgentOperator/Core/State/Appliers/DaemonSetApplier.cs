// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers;

[UsedImplicitly]
public class DaemonSetApplier : BaseApplier<V1DaemonSet, DaemonSetResource>
{
    public DaemonSetApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
    {
    }

    public override ValueTask<DaemonSetResource> CreateFrom(V1DaemonSet entity, CancellationToken cancellationToken = default)
    {
        var resource = new DaemonSetResource(
            entity.Uid(),
            entity.Metadata.GetLabels(),
            entity.Spec.Template.GetPod(),
            entity.Spec.Selector.ToPodSelector()
        );
        return ValueTask.FromResult(resource);
    }
}
