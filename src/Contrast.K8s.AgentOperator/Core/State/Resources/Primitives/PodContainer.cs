using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record PodContainer(
        string Name,
        string Image,
        IReadOnlyCollection<ContainerEnvironmentVariable> Environment,
        IReadOnlyCollection<ContainerVolumeMount> VolumeMounts
    );

    public record ContainerEnvironmentVariable(string Name, string? Value = null, bool HasExternalValue = false);
    public record ContainerVolumeMount(string Name, string MountPath);
}
