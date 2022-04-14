using Contrast.K8s.AgentOperator.Core;
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
