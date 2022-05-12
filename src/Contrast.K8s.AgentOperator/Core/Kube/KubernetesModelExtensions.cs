using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Kube
{
    public static class KubernetesModelExtensions
    {
        public static IReadOnlyCollection<MetadataLabel> GetLabels(this V1ObjectMeta meta)
        {
            if (meta.Labels == null)
            {
                return Array.Empty<MetadataLabel>();
            }

            return meta.Labels.Select(x => new MetadataLabel(x.Key, x.Value)).ToList();
        }

        public static IReadOnlyCollection<MetadataAnnotations> GetAnnotations(this V1ObjectMeta meta)
        {
            if (meta.Annotations == null)
            {
                return Array.Empty<MetadataAnnotations>();
            }

            return meta.Annotations.Select(x => new MetadataAnnotations(x.Key, x.Value)).ToList();
        }

        public static PodTemplate GetPod(this V1PodTemplateSpec spec)
        {
            return new PodTemplate(
                spec.Metadata.GetLabels(),
                spec.Metadata.GetAnnotations(),
                spec.GetContainers()
            );
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
                           specContainer.Image
                       ))
                       .ToList();
        }
    }
}
