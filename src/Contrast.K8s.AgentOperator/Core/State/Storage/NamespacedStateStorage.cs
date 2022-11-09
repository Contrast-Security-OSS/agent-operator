// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Contrast.K8s.AgentOperator.Core.State.Storage
{
    public class NamespacedStateStorage : IEnumerable<(NamespacedResourceIdentity Identity, ResourceHolder Holder)>
    {
        private readonly ByType _byType = new();

        public ResourceHolder this[NamespacedResourceIdentity key]
        {
            get => _byType[key.Type][key.Namespace][key.Name].Holder;
            set
            {
                if (!_byType.TryGetValue(key.Type, out var byNamespace))
                {
                    byNamespace = _byType[key.Type] = new ByNamespace();
                }

                if (!byNamespace.TryGetValue(key.Namespace, out var byName))
                {
                    byName = byNamespace[key.Namespace] = new ByName();
                }

                byName[key.Name] = (key, value);
            }
        }

        public IEnumerable<(NamespacedResourceIdentity Identity, ResourceHolder Holder)> GetByType(Type type)
        {
            var query = from byType in _byType.Where(x => x.Key.IsAssignableTo(type))
                        from byNamespace in byType.Value
                        from byName in byNamespace.Value
                        select (byName.Value.Identity, byName.Value.Holder);
            return query;
        }

        public IEnumerable<(NamespacedResourceIdentity Identity, ResourceHolder Holder)> GetByTypeAndNamespace(Type type, string @namespace)
        {
            var query = from byType in _byType.Where(x => x.Key.IsAssignableTo(type))
                        from byNamespace in byType.Value.Where(x => string.Equals(x.Key, @namespace, StringComparison.OrdinalIgnoreCase))
                        from byName in byNamespace.Value
                        select (byName.Value.Identity, byName.Value.Holder);
            return query;
        }

        public bool TryGetValue(NamespacedResourceIdentity identity, [NotNullWhen(true)] out ResourceHolder? value)
        {
            if (_byType.TryGetValue(identity.Type, out var byNamespace)
                && byNamespace.TryGetValue(identity.Namespace, out var byName)
                && byName.TryGetValue(identity.Name, out var item))
            {
                value = item.Holder;
                return true;
            }

            value = default;
            return false;
        }

        public bool ContainsKey(NamespacedResourceIdentity identity)
        {
            return _byType.TryGetValue(identity.Type, out var byNamespace)
                   && byNamespace.TryGetValue(identity.Namespace, out var byName)
                   && byName.ContainsKey(identity.Name);
        }

        public bool Remove(NamespacedResourceIdentity identity)
        {
            if (_byType.TryGetValue(identity.Type, out var byNamespace))
            {
                if (byNamespace.TryGetValue(identity.Namespace, out var byName))
                {
                    return byName.Remove(identity.Name);
                }
            }

            // TODO Cleanup old sparse dictionaries.
            return false;
        }

        public IEnumerator<(NamespacedResourceIdentity Identity, ResourceHolder Holder)> GetEnumerator()
        {
            var query = from byType in _byType
                        from byNamespace in byType.Value
                        from byName in byNamespace.Value
                        select (byName.Value.Identity, byName.Value.Holder);
            return query.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class ByType : Dictionary<Type, ByNamespace>
        {
        }

        private class ByNamespace : Dictionary<string, ByName>
        {
            public ByNamespace() : base(StringComparer.OrdinalIgnoreCase)
            {
            }
        }

        private class ByName : Dictionary<string, (NamespacedResourceIdentity Identity, ResourceHolder Holder)>
        {
            public ByName() : base(StringComparer.OrdinalIgnoreCase)
            {
            }
        }
    }
}
