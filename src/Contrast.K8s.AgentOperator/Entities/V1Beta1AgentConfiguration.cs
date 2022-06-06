using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Entities
{
    [KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "AgentConfiguration", PluralName = "agentconfigurations")]
    [EntityRbac(typeof(V1Beta1AgentConfiguration), Verbs = VerbConstants.FullControl)]
    public class V1Beta1AgentConfiguration : CustomKubernetesEntity<V1Beta1AgentConfiguration.AgentConfigurationSpec>
    {
        public class AgentConfigurationSpec
        {
            /// <summary>
            /// The contrast_security.yaml file. Multiple lines are supported.
            /// Required.
            /// </summary>
            [Required]
            public string Yaml { get; set; } = string.Empty;
        }
    }
}
