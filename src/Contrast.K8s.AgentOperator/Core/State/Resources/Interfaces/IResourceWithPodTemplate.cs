using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces
{
    public interface IResourceWithPodTemplate : INamespacedResource, IMutableResource
    {
        IReadOnlyCollection<MetadataLabel> Labels { get; }
        IReadOnlyCollection<MetadataAnnotations> Annotations { get; }
        PodTemplate PodTemplate { get; }
    }
}
