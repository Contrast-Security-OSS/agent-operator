// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Kube;

public static class KubernetesClientExtensions
{
    public static async ValueTask PatchEntity<T>(this IKubernetesClient client, T resource, string patch, string fieldManager)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        var apiClient = client.ApiClient;
        var crd = EntityMetadataCache.GetMetadata<T>();
        var crPatch = new V1Patch(patch, V1Patch.PatchType.JsonPatch);

        if (string.IsNullOrWhiteSpace(resource.Metadata.NamespaceProperty))
        {
            using var result = await apiClient.CustomObjects.PatchClusterCustomObjectWithHttpMessagesAsync(
                crPatch,
                crd.Group,
                crd.Version,
                crd.Plural,
                resource.Metadata.Name,
                fieldManager: fieldManager
            );
        }
        else
        {
            using var result = await apiClient.CustomObjects.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                crPatch,
                crd.Group,
                crd.Version,
                resource.Metadata.NamespaceProperty,
                crd.Plural,
                resource.Metadata.Name,
                fieldManager: fieldManager
            );
        }
    }

    public static async ValueTask PatchStatus<T>(this IKubernetesClient client,
                                                 string name,
                                                 string? @namespace,
                                                 GenericCondition condition,
                                                 string fieldManager)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        var apiClient = client.ApiClient;
        var crd = EntityMetadataCache.GetMetadata<T>();

        var body = new StrategicMergePatchBase
        {
            Status = new StrategicMergePatchBase.StatusBase
            {
                Conditions = new List<GenericCondition>(1)
                {
                    condition
                }
            }
        };
        var crPatch = new V1Patch(KubernetesJson.Serialize(body), V1Patch.PatchType.StrategicMergePatch);

        if (string.IsNullOrWhiteSpace(@namespace))
        {
            using var result = await apiClient.CustomObjects.PatchClusterCustomObjectStatusWithHttpMessagesAsync(
                crPatch,
                crd.Group,
                crd.Version,
                crd.Plural,
                name,
                fieldManager: fieldManager
            );
        }
        else
        {
            using var result = await apiClient.CustomObjects.PatchNamespacedCustomObjectStatusWithHttpMessagesAsync(
                crPatch,
                crd.Group,
                crd.Version,
                @namespace,
                crd.Plural,
                name,
                fieldManager: fieldManager
            );
        }
    }

    private class StrategicMergePatchBase
    {
        [JsonPropertyName("status")]
        public StatusBase? Status { get; set; }

        public class StatusBase
        {
            [JsonPropertyName("conditions")]
            public IReadOnlyList<GenericCondition> Conditions { get; set; } = Array.Empty<GenericCondition>();
        }
    }
}

public class GenericCondition
{
    /// <summary>
    /// Last time we probed the condition.
    /// </summary>
    [JsonPropertyName("lastProbeTime")]
    public DateTime? LastProbeTime { get; set; }

    /// <summary>
    /// Last time the condition transitioned from one status to another.
    /// </summary>
    [JsonPropertyName("lastTransitionTime")]
    public DateTime? LastTransitionTime { get; set; }

    /// <summary>
    /// Human-readable message indicating details about last transition.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Unique, one-word, CamelCase reason for the condition&apos;s last transition.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Status is the status of the condition. Can be True, False, Unknown. More info:
    /// https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle#pod-conditions
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Type is the type of the condition. More info:
    /// https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle#pod-conditions
    /// 
    /// Possible enum values:
    /// - `&quot;ContainersReady&quot;` indicates whether all containers in the pod are ready.
    /// - `&quot;Initialized&quot;` means that all init containers in the pod have started
    /// successfully.
    /// - `&quot;PodScheduled&quot;` represents status of the scheduling process for this pod.
    /// - `&quot;Ready&quot;` means the pod is able to service requests and should be added to the
    /// load balancing pools of all matching services.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
