using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record PodTemplate(IReadOnlyCollection<MetadataLabel> Labels,
                              IReadOnlyCollection<MetadataAnnotations> Annotations,
                              IReadOnlyCollection<PodContainer> Containers);
}
