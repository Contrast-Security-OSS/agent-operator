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

        ValueTask<T?> GetById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask<bool> ExistsById<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask<IReadOnlyCollection<ResourceIdentityPair<T>>> GetByType<T>(CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask<IReadOnlyCollection<NamespacedResourceIdentity>> GetKeysByType<T>(CancellationToken cancellationToken = default)
            where T : class, INamespacedResource;

        ValueTask MarkAsDirty(NamespacedResourceIdentity identity,
                              CancellationToken cancellationToken = default);

        ValueTask MarkAsDirty<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource;

        ValueTask<bool> GetIsDirty(NamespacedResourceIdentity identity,
                                   CancellationToken cancellationToken = default);

        ValueTask<bool> GetIsDirty<T>(string name, string @namespace, CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource;
    }

    public class StateContainer : IStateContainer
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Dictionary<NamespacedResourceIdentity, ResourceHolder> _resources = new();
        private readonly IResourceComparer _resourceComparer;

        public StateContainer(IResourceComparer resourceComparer)
        {
            _resourceComparer = resourceComparer;
        }

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
                    if (_resourceComparer.AreEqual(existing.Resource, resource))
                    {
                        // No change.
                        return new StateUpdateResult<T>(false, null, (T)existing.Resource);
                    }

                    // Updated.
                    _resources[identity] = new ResourceHolder(resource);
                    return new StateUpdateResult<T>(true, (T)existing.Resource, resource);
                }
                else
                {
                    // Added.
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

        public async ValueTask<T?> GetById<T>(string name,
                                              string @namespace,
                                              CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var identity = NamespacedResourceIdentity.Create<T>(name, @namespace);
                if (_resources.TryGetValue(identity, out var ret))
                {
                    return (T)ret.Resource;
                }

                return default;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<bool> ExistsById<T>(string name,
                                                   string @namespace,
                                                   CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _resources.ContainsKey(NamespacedResourceIdentity.Create<T>(name, @namespace));
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<IReadOnlyCollection<ResourceIdentityPair<T>>> GetByType<T>(CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _resources.Where(x => x.Key.Type.IsAssignableTo(typeof(T)))
                                 .Select(x => new ResourceIdentityPair<T>(x.Key, (T)x.Value.Resource))
                                 .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<IReadOnlyCollection<NamespacedResourceIdentity>> GetKeysByType<T>(CancellationToken cancellationToken = default)
            where T : class, INamespacedResource
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _resources.Where(x => x.Key.Type.IsAssignableTo(typeof(T)))
                                 .Select(x => x.Key)
                                 .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask MarkAsDirty(NamespacedResourceIdentity identity,
                                           CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
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

        public ValueTask MarkAsDirty<T>(string name,
                                        string @namespace,
                                        CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource
        {
            return MarkAsDirty(NamespacedResourceIdentity.Create<T>(name, @namespace), cancellationToken);
        }

        public async ValueTask<bool> GetIsDirty(NamespacedResourceIdentity identity,
                                                CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _resources[identity].IsDirty;
            }
            finally
            {
                _lock.Release();
            }
        }

        public ValueTask<bool> GetIsDirty<T>(string name,
                                             string @namespace,
                                             CancellationToken cancellationToken = default)
            where T : class, INamespacedResource, IMutableResource
        {
            return GetIsDirty(NamespacedResourceIdentity.Create<T>(name, @namespace), cancellationToken);
        }

        private record ResourceHolder(INamespacedResource Resource, bool IsDirty = false);
    }

    public record ResourceIdentityPair<T>(NamespacedResourceIdentity Identity, T Resource);

    public record StateUpdateResult<T>(bool Modified, T? Previous, T? Current) where T : class, INamespacedResource;
}
