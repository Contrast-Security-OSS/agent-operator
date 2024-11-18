// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Rbac;

namespace Contrast.K8s.AgentOperator.Entities.Dynatrace;

[KubernetesEntity(Group = "dynatrace.com", ApiVersion = "v1beta1", Kind = "DynaKube", PluralName = "dynakubes")]
[EntityRbac(typeof(V1Beta1DynaKube), Verbs = VerbConstants.ReadOnly)]
public partial class V1Beta1DynaKube : CustomKubernetesEntity<V1Beta1DynaKube.DynaKubeSpec>
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
