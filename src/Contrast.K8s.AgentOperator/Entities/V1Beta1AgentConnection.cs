// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;
using KubeOps.Abstractions.Rbac;

namespace Contrast.K8s.AgentOperator.Entities;

[KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "AgentConnection", PluralName = "agentconnections")]
[EntityRbac(typeof(V1Beta1AgentConnection), Verbs = VerbConstants.FullControl)]
public partial class V1Beta1AgentConnection : CustomKubernetesEntity<V1Beta1AgentConnection.AgentInjectorSpec>
{
    public class AgentInjectorSpec
    {
        [Description("The Token to use for this connection.")]
        public SecretRef? Token { get; set; } = new();

        [Description("The URL of the Contrast server. Defaults to 'https://app-agents.contrastsecurity.com/Contrast'.")]
        public string? Url { get; set; }

        [Description("The API Key to use for this connection.")]
        public SecretRef? ApiKey { get; set; } = new();

        [Description("The Service Key to use for this connection.")]
        public SecretRef? ServiceKey { get; set; } = new();

        [Description("The User Name to use for this connection.")]
        public SecretRef? UserName { get; set; } = new();
    }

    public class SecretRef
    {
        [Required]
        [Description("The name of the secret to reference. Must exist in the same namespace as the AgentConnection. Required.")]
        public string SecretName { get; set; } = null!;

        [Required]
        [Description("The key in the secret to access the value for. Must exist in the same namespace as the AgentConnection. Required.")]
        public string SecretKey { get; set; } = null!;
    }
}
