using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Beta1ClusterAgentConnection), Verbs = VerbConstants.ReadOnly), UsedImplicitly]
    public class ClusterAgentConnectionController : GenericController<V1Beta1ClusterAgentConnection>
    {
        public ClusterAgentConnectionController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
