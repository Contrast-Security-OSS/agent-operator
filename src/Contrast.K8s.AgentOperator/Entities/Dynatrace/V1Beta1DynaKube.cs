// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Entities;

namespace Contrast.K8s.AgentOperator.Entities.Dynatrace
{
    [KubernetesEntity(Group = "dynatrace.com", ApiVersion = "v1beta1", Kind = "DynaKube", PluralName = "dynakubes"), UsedImplicitly]
    public class V1Beta1DynaKube : CustomKubernetesEntity<V1Beta1DynaKube.DynaKubeSpec>
    {
        /// <summary>
        /// DynaKubeSpec represents the dynatrace operator config
        /// </summary>
        public class DynaKubeSpec
        {
            /// <summary>
            /// OneAgent Config
            /// </summary>
            [JsonPropertyName("oneAgent")]
            public OneAgentSpec? OneAgent { get; set; }
        }
    }

    public class OneAgentSpec
    {
        [JsonPropertyName("classicFullStack")]
        public EmptyObject? ClassicFullStack { get; set; }

        [JsonPropertyName("applicationMonitoring")]
        public EmptyObject? ApplicationMonitoring { get; set; }

        [JsonPropertyName("hostMonitoring")]
        public EmptyObject? HostMonitoring { get; set; }

        [JsonPropertyName("cloudNativeFullStack")]
        public EmptyObject? CloudNativeFullStack { get; set; }

        public class EmptyObject
        {
        }
    }
}
