// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Newtonsoft.Json.Linq;

namespace Contrast.K8s.AgentOperator.Core.Kube
{
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

        public JToken ToJToken<T>(T entity)
        {
            return JToken.Parse(SerializeObject(entity));
        }

        public T DeepClone<T>(T entity)
        {
            return DeserializeObject<T>(SerializeObject(entity));
        }
    }
}
