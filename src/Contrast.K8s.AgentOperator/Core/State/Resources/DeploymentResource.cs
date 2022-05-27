using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record DeploymentResource(IReadOnlyCollection<MetadataLabel> Labels,
                                     IReadOnlyCollection<MetadataAnnotations> Annotations,
                                     PodTemplate PodTemplate,
                                     PodSelector Selector)
        : IResourceWithPodTemplate;
}
