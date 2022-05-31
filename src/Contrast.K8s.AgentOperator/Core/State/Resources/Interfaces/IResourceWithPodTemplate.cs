using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces
{
    public interface IResourceWithPodTemplate : INamespacedResource, IMutableResource
    {
        string Uid { get; }
        IReadOnlyCollection<MetadataLabel> Labels { get; }
        PodTemplate PodTemplate { get; }
        PodSelector Selector { get; }
    }
}
