using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record DeploymentResource(
        IReadOnlyCollection<MetadataLabel> Labels
    ) : NamespacedResource;
}
