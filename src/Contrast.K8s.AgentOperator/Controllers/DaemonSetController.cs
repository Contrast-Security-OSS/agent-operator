using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1DaemonSet), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class DaemonSetController : GenericController<V1DaemonSet>
    {
        public DaemonSetController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
