// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using JetBrains.Annotations;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public abstract record NamespacedResourceIdentity([UsedImplicitly] string Name, [UsedImplicitly] string Namespace, [UsedImplicitly] Type Type)
    {
        public static IEqualityComparer<NamespacedResourceIdentity> Comparer { get; } = new CaseInsensitiveEqualityComparer();

        public static NamespacedResourceIdentity<T> Create<T>(string name, string @namespace) where T : INamespacedResource
        {
            return new NamespacedResourceIdentity<T>(name, @namespace);
        }

        public override string ToString()
        {
            return $"{Type.Name}/{Namespace}/{Name}";
        }

        private sealed class CaseInsensitiveEqualityComparer : IEqualityComparer<NamespacedResourceIdentity>
        {
            public bool Equals(NamespacedResourceIdentity? x, NamespacedResourceIdentity? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null))
                {
                    return false;
                }

                if (ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(x.Namespace, y.Namespace, StringComparison.OrdinalIgnoreCase)
                       && x.Type == y.Type;
            }

            public int GetHashCode(NamespacedResourceIdentity obj)
            {
                var hashCode = new HashCode();

                hashCode.Add(obj.Name, StringComparer.OrdinalIgnoreCase);
                hashCode.Add(obj.Namespace, StringComparer.OrdinalIgnoreCase);
                hashCode.Add(obj.Type);

                return hashCode.ToHashCode();
            }
        }
    }

    public record NamespacedResourceIdentity<T>(string Name, string Namespace)
        : NamespacedResourceIdentity(Name, Namespace, typeof(T))
        where T : INamespacedResource
    {
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
