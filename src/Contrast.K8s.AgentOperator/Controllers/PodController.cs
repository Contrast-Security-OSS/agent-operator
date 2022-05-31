using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
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
        private readonly IEventStream _eventStream;

        public PodController(IEventStream eventStream) : base(eventStream)
        {
            _eventStream = eventStream;
        }

        public override async Task StatusModifiedAsync(V1Pod entity)
        {
            await _eventStream.DispatchDeferred(new EntityReconciled<V1Pod>(entity));
        }
    }
}
