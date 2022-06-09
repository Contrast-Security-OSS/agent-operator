using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Entities
{
    [KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "AgentConfiguration", PluralName = "agentconfigurations")]
    [EntityRbac(typeof(V1Beta1AgentConfiguration), Verbs = VerbConstants.FullControl)]
    public class V1Beta1AgentConfiguration : CustomKubernetesEntity<V1Beta1AgentConfiguration.AgentConfigurationSpec>
    {
        public V1Beta1AgentConfiguration()
        {
            Kind = "AgentConfiguration";
            ApiVersion = "agents.contrastsecurity.com/v1beta1";
        }

        public class AgentConfigurationSpec
        {
            /// <summary>
            /// The contrast_security.yaml file. Multiple lines are supported.
            /// </summary>
            public string? Yaml { get; set; }
        }
    }
}
