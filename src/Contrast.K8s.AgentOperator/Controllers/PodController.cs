using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Pod), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class PodController : GenericController<V1Pod>
    {
        public PodController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
