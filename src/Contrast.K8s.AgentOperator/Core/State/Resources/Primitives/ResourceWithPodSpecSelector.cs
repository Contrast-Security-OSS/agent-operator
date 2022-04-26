using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record ResourceWithPodSpecSelector(
        IReadOnlyCollection<string> ImagesPatterns,
        IReadOnlyDictionary<string, string> LabelPatterns,
        IReadOnlyCollection<string> Namespaces);
}
