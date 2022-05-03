using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Beta1AgentInjector), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class AgentInjectorController : GenericController<V1Beta1AgentInjector>
    {
        public AgentInjectorController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
