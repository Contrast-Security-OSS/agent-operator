using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IStateContainer
    {
        ValueTask<StateUpdateResult<T>> AddOrReplaceById<T>(string name, string @namespace, T resource, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask<StateUpdateResult<T>> RemoveById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask<T> GetById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask<IReadOnlyCollection<T>> GetByType<T>(CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask MarkAsDirty<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource;

        ValueTask<bool> GetIsDirty<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource;
    }

    public class StateContainer : IStateContainer
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Dictionary<NamespacedResourceIdentity, ResourceHolder> _resources = new();

        public async ValueTask<StateUpdateResult<T>> AddOrReplaceById<T>(string name,
                                                                         string @namespace,
                                                                         T resource,
                                                                         CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var identity = NamespacedResourceIdentity.Create<T>(name, @namespace);
                if (_resources.TryGetValue(identity, out var existing))
                {
                    if (existing.Resource.Equals(resource))
                    {
                        // No change.
                        return new StateUpdateResult<T>(false, null, (T)existing.Resource);
                    }

                    _resources[identity] = new ResourceHolder(resource);
                    return new StateUpdateResult<T>(true, (T)existing.Resource, resource);
                }
                else
                {
                    _resources[identity] = new ResourceHolder(resource);
                    return new StateUpdateResult<T>(true, null, resource);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<StateUpdateResult<T>> RemoveById<T>(string name,
                                                                   string @namespace,
                                                                   CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var identity = NamespacedResourceIdentity.Create<T>(name, @namespace);
                if (_resources.TryGetValue(identity, out var existing))
                {
                    _resources.Remove(identity);
                    return new StateUpdateResult<T>(true, (T)existing.Resource, null);
                }

                return new StateUpdateResult<T>(false, null, null);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<T> GetById<T>(string name,
                                             string @namespace,
                                             CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return (T)_resources[NamespacedResourceIdentity.Create<T>(name, @namespace)].Resource;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<IReadOnlyCollection<T>> GetByType<T>(CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _resources.Where(x => x.Key.Type == typeof(T))
                                 .Select(x => (T)x.Value.Resource)
                                 .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask MarkAsDirty<T>(string name,
                                              string @namespace,
                                              CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var identity = NamespacedResourceIdentity.Create<T>(name, @namespace);
                if (_resources.ContainsKey(identity))
                {
                    _resources[identity] = _resources[identity] with
                    {
                        IsDirty = true
                    };
                }
                else
                {
                    throw new Exception("Cannot mark a resource as dirty that does not exist.");
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<bool> GetIsDirty<T>(string name,
                                                   string @namespace,
                                                   CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var identity = NamespacedResourceIdentity.Create<T>(name, @namespace);
                return _resources[identity].IsDirty;
            }
            finally
            {
                _lock.Release();
            }
        }

        private record ResourceHolder(INamespacedResource Resource, bool IsDirty = false);
    }

    public record StateUpdateResult<T>(bool Modified, T? Previous, T? Current) where T : class, INamespacedResource;
}
