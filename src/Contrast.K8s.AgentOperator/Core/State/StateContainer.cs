using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IStateContainer
    {
        ValueTask<StateUpdateResult<T>> AddOrReplaceById<T>(string name, string @namespace, T resource, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask<StateUpdateResult<T>> RemoveById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask<T> GetById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask<IReadOnlyCollection<T>> GetByType<T>(CancellationToken cancellationToken = default)
            where T : NamespacedResource;
    }

    public class StateContainer : IStateContainer
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Dictionary<NamespacedResourceIdentity, NamespacedResource> _resources = new();

        public async ValueTask<StateUpdateResult<T>> AddOrReplaceById<T>(string name, string @namespace, T resource,
                                                                         CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var identity = NamespacedResourceIdentity.Create<T>(name, @namespace);
                if (_resources.TryGetValue(identity, out var existing))
                {
                    if (!existing.Equals(resource))
                    {
                        _resources[identity] = resource;
                        return new StateUpdateResult<T>(true, (T)existing, resource);
                    }

                    return new StateUpdateResult<T>(false, null, (T)existing);
                }
                else
                {
                    _resources[identity] = resource;
                    return new StateUpdateResult<T>(true, null, resource);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<StateUpdateResult<T>> RemoveById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var identity = NamespacedResourceIdentity.Create<T>(name, @namespace);
                if (_resources.TryGetValue(identity, out var existing))
                {
                    _resources.Remove(identity);
                    return new StateUpdateResult<T>(true, (T)existing, null);
                }

                return new StateUpdateResult<T>(false, null, null);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<T> GetById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return (T)_resources[NamespacedResourceIdentity.Create<T>(name, @namespace)];
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<IReadOnlyCollection<T>> GetByType<T>(CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _resources.Where(x => x.Key.Type == typeof(T))
                                 .Select(x => (T)x.Value)
                                 .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    public record StateUpdateResult<T>(bool Modified, T? Previous, T? Current) where T : NamespacedResource;
}
