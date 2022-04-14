using Contrast.K8s.AgentOperator.Core;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1StatefulSet), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class StatefulSetController : GenericController<V1StatefulSet>
    {
        public StatefulSetController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
