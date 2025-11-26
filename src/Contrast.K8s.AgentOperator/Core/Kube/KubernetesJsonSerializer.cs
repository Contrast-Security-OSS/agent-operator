// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Json.More;
using Json.Patch;
using k8s;
using System.Text.Json.Nodes;

namespace Contrast.K8s.AgentOperator.Core.Kube;

public class KubernetesJsonSerializer
{
    public string SerializeObject<T>(T entity)
    {
        return KubernetesJson.Serialize(entity);
    }

    public T DeserializeObject<T>(string json)
    {
        return KubernetesJson.Deserialize<T>(json);
    }

    public JsonNode? ToJsonNode<T>(T entity)
    {
        return JsonNode.Parse(SerializeObject(entity));
    }

    public string ToJsonString(JsonPatch patch)
    {
        return patch.ToJsonDocument().RootElement.GetRawText();
    }

    public T DeepClone<T>(T entity)
    {
        return DeserializeObject<T>(SerializeObject(entity));
    }
}
