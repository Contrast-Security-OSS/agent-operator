using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces
{
    public interface IResourceWithPodSpec : INamespacedResource, IMutableResource
    {
        IReadOnlyCollection<MetadataLabel> Labels { get; }

        IReadOnlyCollection<PodContainer> Containers { get; }
        IReadOnlyCollection<PodVolume> Volumes { get; }
    }

    public record PodVolume(string Name, PodVolumeType Type);

    public enum PodVolumeType
    {
        Unknown = 0,
        EmptyDirectory
    }
}
