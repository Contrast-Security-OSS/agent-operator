using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Deployment), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class DeploymentController : GenericController<V1Deployment>
    {
        public DeploymentController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
