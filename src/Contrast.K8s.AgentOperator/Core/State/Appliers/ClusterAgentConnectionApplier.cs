// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class ClusterAgentConnectionApplier : BaseApplier<V1Beta1ClusterAgentConnection, ClusterAgentConnectionResource>
    {
        private readonly AgentConnectionApplier _agentConnectionApplier;

        public ClusterAgentConnectionApplier(IStateContainer stateContainer, IMediator mediator, AgentConnectionApplier agentConnectionApplier) : base(
            stateContainer, mediator)
        {
            _agentConnectionApplier = agentConnectionApplier;
        }

        public override async ValueTask<ClusterAgentConnectionResource> CreateFrom(V1Beta1ClusterAgentConnection entity,
                                                                                   CancellationToken cancellationToken = default)
        {
            entity.Spec.Template!.Metadata.NamespaceProperty = entity.Namespace();

            var template = await _agentConnectionApplier.CreateFrom(entity.Spec.Template!, cancellationToken);
            var namespaces = entity.Spec.Namespaces;

            return new ClusterAgentConnectionResource(template, namespaces);
        }
    }
}
