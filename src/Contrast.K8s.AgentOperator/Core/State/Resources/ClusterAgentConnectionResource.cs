using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record ClusterAgentConnectionResource(
        AgentConnectionResource Template,
        IReadOnlyCollection<string> Namespaces
    ) : INamespacedResource;
}
