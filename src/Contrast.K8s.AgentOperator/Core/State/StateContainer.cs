using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IStateContainer
    {
        ValueTask AddOrReplaceById<T>(string name, string @namespace, T resource, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask AddOrReplaceById<T>(NamespacedResourceIdentity<T> identity, T resource, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask RemoveById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask RemoveById<T>(NamespacedResourceIdentity<T> identity, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask<T> GetById<T>(NamespacedResourceIdentity<T> identity, CancellationToken cancellationToken = default)
            where T : NamespacedResource;

        ValueTask<IReadOnlyCollection<T>> GetByType<T>(CancellationToken cancellationToken = default)
            where T : NamespacedResource;
    }

    public class StateContainer : IStateContainer
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Dictionary<NamespacedResourceIdentity, NamespacedResource> _resources = new();

        public ValueTask AddOrReplaceById<T>(string name, string @namespace, T resource, CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            return AddOrReplaceById(NamespacedResourceIdentity.Create<T>(name, @namespace), resource, cancellationToken);
        }

        public async ValueTask AddOrReplaceById<T>(NamespacedResourceIdentity<T> identity, T resource, CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _resources[identity] = resource;
            }
            finally
            {
                _lock.Release();
            }
        }

        public ValueTask RemoveById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            return RemoveById(NamespacedResourceIdentity.Create<T>(name, @namespace), cancellationToken);
        }

        public async ValueTask RemoveById<T>(NamespacedResourceIdentity<T> identity, CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _resources.Remove(identity);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<T> GetById<T>(NamespacedResourceIdentity<T> identity, CancellationToken cancellationToken = default)
            where T : NamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return (T)_resources[identity];
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
                                 .Select(x => x.Value)
                                 .Cast<T>()
                                 .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
