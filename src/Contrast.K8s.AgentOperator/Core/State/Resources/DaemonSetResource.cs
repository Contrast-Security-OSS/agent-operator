using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record DaemonSetResource(string Uid,
                                    IReadOnlyCollection<MetadataLabel> Labels,
                                    PodTemplate PodTemplate,
                                    PodSelector Selector)
        : IResourceWithPodTemplate;
}
