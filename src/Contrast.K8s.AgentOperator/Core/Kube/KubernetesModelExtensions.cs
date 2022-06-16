// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public static PodTemplate GetPod(this V1PodTemplateSpec? spec)
        {
            if (spec == null)
            {
                return new PodTemplate(
                    Array.Empty<MetadataLabel>(),
                    Array.Empty<MetadataAnnotations>(),
                    Array.Empty<PodContainer>()
                );
            }

            return new PodTemplate(
                spec.Metadata.GetLabels(),
                spec.Metadata.GetAnnotations(),
                spec.GetContainers()
            );
        }

        public static PodSelector ToPodSelector(this V1LabelSelector? spec)
        {
            if (spec == null)
            {
                return new PodSelector(Array.Empty<PodMatchExpression>());
            }

            var expressions = new List<PodMatchExpression>();
            if (spec.MatchLabels is { } matchLabels)
            {
                foreach (var matchLabel in matchLabels)
                {
                    expressions.Add(new PodMatchExpression(matchLabel.Key, LabelMatchOperation.In, new List<string>
                    {
                        matchLabel.Value
                    }));
                }
            }

            if (spec.MatchExpressions is { } matchExpressions)
            {
                foreach (var matchExpression in matchExpressions)
                {
                    var operation = LabelMatchOperation.Unknown;
                    if (string.Equals(matchExpression.OperatorProperty, "In", StringComparison.OrdinalIgnoreCase))
                    {
                        operation = LabelMatchOperation.In;
                    }

                    if (string.Equals(matchExpression.OperatorProperty, "NotIn", StringComparison.OrdinalIgnoreCase))
                    {
                        operation = LabelMatchOperation.NotIn;
                    }

                    if (string.Equals(matchExpression.OperatorProperty, "Exists", StringComparison.OrdinalIgnoreCase))
                    {
                        operation = LabelMatchOperation.Exists;
                    }

                    if (string.Equals(matchExpression.OperatorProperty, "DoesNotExist", StringComparison.OrdinalIgnoreCase))
                    {
                        operation = LabelMatchOperation.DoesNotExist;
                    }

                    var values = matchExpression.Values?.ToList() ?? (IReadOnlyCollection<string>)Array.Empty<string>();
                    expressions.Add(new PodMatchExpression(matchExpression.Key, operation, values));
                }
            }

            return new PodSelector(expressions);
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
