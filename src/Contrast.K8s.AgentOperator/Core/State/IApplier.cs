using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using k8s;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IApplier<T> : IRequestHandler<EntityReconciled<T>>, IRequestHandler<EntityDeleted<T>> where T : IKubernetesObject<V1ObjectMeta>
    {
    }

    public abstract class BaseApplier<TKubernetesObject, TResource> : IApplier<TKubernetesObject>
        where TKubernetesObject : IKubernetesObject<V1ObjectMeta>
        where TResource : class, INamespacedResource
    {
        // ReSharper disable once InconsistentNaming
        private readonly Logger Logger = LogManager.GetLogger(typeof(BaseApplier<,>).FullName + ":" + typeof(TResource).Name);

        private readonly IStateContainer _stateContainer;
        private readonly IMediator _mediator;

        protected BaseApplier(IStateContainer stateContainer, IMediator mediator)
        {
            _stateContainer = stateContainer;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(EntityReconciled<TKubernetesObject> request, CancellationToken cancellationToken)
        {
            var entity = request.Entity;
            var resource = await CreateFrom(entity, cancellationToken);

            var name = entity.Name();
            var ns = entity.Namespace();

            var (modified, previous, current) = await _stateContainer.AddOrReplaceById(name, ns, resource, cancellationToken);
            if (modified)
            {
                Logger.Debug($"Resource '{ns}/{name}' of kind '{typeof(TResource).Name}'' was reconciled.");
                await _mediator.Publish(StateModified.Create(previous, current), cancellationToken);
            }

            return Unit.Value;
        }

        public async Task<Unit> Handle(EntityDeleted<TKubernetesObject> request, CancellationToken cancellationToken)
        {
            var entity = request.Entity;
            var name = entity.Name();
            var ns = entity.Namespace();
            var (modified, previous, current) = await _stateContainer.RemoveById<TResource>(name, ns, cancellationToken);
            if (modified)
            {
                Logger.Debug($"Resource '{ns}/{name}' of type '{typeof(TResource).Name}' was deleted.");
                await _mediator.Publish(StateModified.Create(previous, current), cancellationToken);
            }

            return Unit.Value;
        }

        protected abstract ValueTask<TResource> CreateFrom(TKubernetesObject entity, CancellationToken cancellationToken = default);
    }
}
