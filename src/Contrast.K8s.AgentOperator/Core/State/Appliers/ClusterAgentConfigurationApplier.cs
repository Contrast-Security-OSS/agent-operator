using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
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
            var template = await _agentConfigurationApplier.CreateFrom(entity.Spec.Template!, cancellationToken);
            var namespaces = entity.Spec.Namespaces;

            return new ClusterAgentConfigurationResource(template, namespaces);
        }
    }
}
