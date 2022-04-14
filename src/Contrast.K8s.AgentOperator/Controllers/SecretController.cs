using Contrast.K8s.AgentOperator.Core;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Secret), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class SecretController : GenericController<V1Secret>
    {
        public SecretController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}