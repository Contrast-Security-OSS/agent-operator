using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Kube
{
    public static class KubernetesModelExtensions
    {
        public static IReadOnlyCollection<MetadataLabel> GetLabels(this V1ObjectMeta meta)
        {
            return meta.EnsureLabels().Select(x => new MetadataLabel(x.Key, x.Value)).ToList();
        }

        public static IReadOnlyCollection<ContainerEnvironmentVariable> GetEnvironmentVariables(this V1Container container)
        {
            if (container.Env == null)
            {
                return Array.Empty<ContainerEnvironmentVariable>();
            }

            return container.Env.Select(x => new ContainerEnvironmentVariable(x.Name, x.Value, x.ValueFrom != null)).ToList();
        }

        public static IReadOnlyCollection<ContainerVolumeMount> GetVolumeMount(this V1Container container)
        {
            if (container.VolumeMounts == null)
            {
                return Array.Empty<ContainerVolumeMount>();
            }

            return container.VolumeMounts.Select(x => new ContainerVolumeMount(x.Name, x.MountPath)).ToList();
        }

        public static IReadOnlyCollection<PodContainer> GetContainers(this V1PodTemplateSpec spec)
        {
            if (spec.Spec.Containers == null)
            {
                return Array.Empty<PodContainer>();
            }

            return spec.Spec.Containers
                       .Select(specContainer => new PodContainer(
                           specContainer.Name,
                           specContainer.Image,
                           specContainer.GetEnvironmentVariables(),
                           specContainer.GetVolumeMount()
                       ))
                       .ToList();
        }

        public static PodVolumeType GetVolumeType(this V1Volume spec)
        {
            if (spec.EmptyDir != null)
            {
                return PodVolumeType.EmptyDirectory;
            }

            return PodVolumeType.Unknown;
        }

        public static IReadOnlyCollection<PodVolume> GetVolumes(this V1PodTemplateSpec spec)
        {
            if (spec.Spec.Volumes == null)
            {
                return Array.Empty<PodVolume>();
            }

            return spec.Spec.Volumes.Select(x => new PodVolume(x.Name, x.GetVolumeType())).ToList();
        }
    }
}
