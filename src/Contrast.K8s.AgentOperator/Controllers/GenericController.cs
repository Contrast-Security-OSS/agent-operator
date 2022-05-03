using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.State;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;

namespace Contrast.K8s.AgentOperator.Controllers
{
    public abstract class GenericController<T> : IResourceController<T> where T : IKubernetesObject<V1ObjectMeta>
    {
        private readonly IEventStream _eventStream;

        protected GenericController(IEventStream eventStream)
        {
            _eventStream = eventStream;
        }

        public async Task<ResourceControllerResult?> ReconcileAsync(T entity)
        {
            await _eventStream.Dispatch(new EntityReconciled<T>(entity));
            return null;
        }

        public async Task DeletedAsync(T entity)
        {
            await _eventStream.Dispatch(new EntityDeleted<T>(entity));
        }
    }
}
