using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record ResourceWithPodSpecSelector(
        IReadOnlyCollection<string> Images,
        IReadOnlyDictionary<string, string> Labels,
        IReadOnlyCollection<string> Namespaces);
}
