using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Beta1ClusterAgentConfiguration), Verbs = VerbConstants.ReadOnly), UsedImplicitly]
    public class ClusterAgentConfigurationController : GenericController<V1Beta1ClusterAgentConfiguration>
    {
        public ClusterAgentConfigurationController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
