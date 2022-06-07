using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces
{
    public interface IClusterResourceTemplate<out T> : INamespacedResource
    {
        public T Template { get; }
        public IReadOnlyCollection<string> NamespacePatterns { get; }
    }
}
