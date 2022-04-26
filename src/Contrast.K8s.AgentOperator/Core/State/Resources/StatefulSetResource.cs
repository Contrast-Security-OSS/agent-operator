using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record StatefulSetResource(IReadOnlyCollection<MetadataLabel> Labels,
                                      IReadOnlyCollection<PodContainer> Containers,
                                      IReadOnlyCollection<PodVolume> Volumes)
        : IResourceWithPodSpec;
}
