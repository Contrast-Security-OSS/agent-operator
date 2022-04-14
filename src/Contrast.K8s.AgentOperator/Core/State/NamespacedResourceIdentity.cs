using System;
using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public abstract record NamespacedResourceIdentity(string Name, string Namespace, Type Type)
    {
        public static NamespacedResourceIdentity<T> Create<T>(string name, string @namespace) where T : NamespacedResource
        {
            return new NamespacedResourceIdentity<T>(name, @namespace);
        }
    }

    public record NamespacedResourceIdentity<T>(string Name, string Namespace)
        : NamespacedResourceIdentity(Name, Namespace, typeof(T))
        where T : NamespacedResource;
}
