using System.Threading.Tasks;
using DotnetKubernetesClient;
using DotnetKubernetesClient.Entities;
using JsonDiffPatch;
using k8s;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Kube
{
    public static class KubernetesClientExtensions
    {
        public static async Task Patch<T>(this IKubernetesClient client, T resource, PatchDocument patchDocument, string fieldManager)
            where T : IKubernetesObject<V1ObjectMeta>
        {
            var apiClient = client.ApiClient;
            var crd = resource.CreateResourceDefinition();
            var crPatch = new V1Patch(patchDocument.ToString(), V1Patch.PatchType.JsonPatch);

            if (string.IsNullOrWhiteSpace(resource.Metadata.NamespaceProperty))
            {
                using var result = await apiClient.PatchClusterCustomObjectWithHttpMessagesAsync(
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
                using var result = await apiClient.PatchNamespacedCustomObjectWithHttpMessagesAsync(
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
    }
}
