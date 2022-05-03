using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Entities
{
    [KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "AgentConnection", PluralName = "agentconnections")]
    [EntityRbac(typeof(V1Beta1AgentConnection), Verbs = VerbConstants.ReadOnly)]
    public class V1Beta1AgentConnection : CustomKubernetesEntity<V1Beta1AgentConnection.AgentInjectorSpec>
    {
        public class AgentInjectorSpec
        {
            /// <summary>
            /// The URL of the Contrast server.
            /// Defaults to 'https://app.contrastsecurity.com/Contrast'.
            /// </summary>
            public string? Url { get; set; }

            /// <summary>
            /// The API Key to use for this connection.
            /// </summary>
            [Required]
            public SecretRef ApiKey { get; set; } = new();

            /// <summary>
            /// The Service Key to use for this connection.
            /// </summary>
            [Required]
            public SecretRef ServiceKey { get; set; } = new();

            /// <summary>
            /// The User Name to use for this connection.
            /// </summary>
            [Required]
            public SecretRef UserName { get; set; } = new();
        }

        public class SecretRef
        {
            /// <summary>
            /// The name of the secret to reference. Must exist in the same namespace as the AgentConnection.
            /// Required.
            /// </summary>
            [Required]
            public string SecretName { get; set; } = null!;

            /// <summary>
            /// The key in the secret to access the value for. Must exist in the same namespace as the AgentConnection.
            /// Required.
            /// </summary>
            [Required]
            public string SecretKey { get; set; } = null!;
        }
    }
}
