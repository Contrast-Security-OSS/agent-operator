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
    public class ClusterAgentConfigurationApplier : BaseApplier<V1Beta1ClusterAgentConfiguration, ClusterAgentConfigurationResource>
    {
        private readonly AgentConfigurationApplier _agentConfigurationApplier;

        public ClusterAgentConfigurationApplier(IStateContainer stateContainer, IMediator mediator, AgentConfigurationApplier agentConfigurationApplier) : base(
            stateContainer, mediator)
        {
            _agentConfigurationApplier = agentConfigurationApplier;
        }

        public override async ValueTask<ClusterAgentConfigurationResource> CreateFrom(V1Beta1ClusterAgentConfiguration entity,
                                                                                      CancellationToken cancellationToken = default)
        {
            entity.Spec.Template!.Metadata.NamespaceProperty = entity.Namespace();

            var template = await _agentConfigurationApplier.CreateFrom(entity.Spec.Template!, cancellationToken);
            var namespaces = entity.Spec.Namespaces;

            return new ClusterAgentConfigurationResource(template, namespaces);
        }
    }
}
