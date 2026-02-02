// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers;

[UsedImplicitly]
public class ClusterAgentInjectorApplier : BaseApplier<V1Beta1ClusterAgentInjector, ClusterAgentInjectorResource>
{
    private readonly AgentInjectorApplier _agentInjectorApplier;

    public ClusterAgentInjectorApplier(IStateContainer stateContainer,
        IMediator mediator,
        AgentInjectorApplier agentInjectorApplier) : base(
        stateContainer, mediator)
    {
        _agentInjectorApplier = agentInjectorApplier;
    }

    public override async ValueTask<ClusterAgentInjectorResource> CreateFrom(V1Beta1ClusterAgentInjector entity,
        CancellationToken cancellationToken = default)
    {
        entity.Spec.Template!.Metadata.NamespaceProperty = entity.Namespace();

        var template = await _agentInjectorApplier.CreateFrom(entity.Spec.Template!, cancellationToken);

        var namespaces = entity.Spec.Namespaces;
        var namespaceLabels = entity.Spec.NamespaceLabelSelector.Select(x => new LabelPattern(x.Name, x.Value)).ToList();

        return new ClusterAgentInjectorResource(template, namespaces, namespaceLabels);
    }
}
