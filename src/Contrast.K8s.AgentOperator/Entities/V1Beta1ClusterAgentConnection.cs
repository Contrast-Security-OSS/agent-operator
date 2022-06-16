// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Entities
{
    [KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "ClusterAgentConnection", PluralName = "clusteragentconnections")]
    [EntityRbac(typeof(V1Beta1ClusterAgentConnection), Verbs = VerbConstants.ReadOnly)]
    public class V1Beta1ClusterAgentConnection : CustomKubernetesEntity<V1Beta1ClusterAgentConnection.ClusterAgentConnectionSpec>
    {
        public class ClusterAgentConnectionSpec
        {
            /// <summary>
            /// The default AgentConnection to apply to the namespaces selected by 'spec.namespaces'.
            /// Required.
            /// </summary>
            [Required]
            public V1Beta1AgentConnection? Template { get; set; }

            /// <summary>
            /// The namespaces to apply this AgentConnection template to. Glob syntax is supported.
            /// Optional, defaults to selecting all namespaces.
            /// </summary>
            public IReadOnlyCollection<string> Namespaces { get; set; } = Array.Empty<string>();
        }
    }
}
