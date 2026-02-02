// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Entities.Common;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;
using KubeOps.Abstractions.Rbac;
using System;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Entities;

[KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "ClusterAgentInjector", PluralName = "clusteragentinjectors")]
[EntityRbac(typeof(V1Beta1ClusterAgentInjector), Verbs = VerbConstants.ReadOnly)]
public partial class V1Beta1ClusterAgentInjector : CustomKubernetesEntity<V1Beta1ClusterAgentInjector.ClusterAgentInjectorSpec>
{
    public class ClusterAgentInjectorSpec
    {
        [Required]
        [Description("The default AgentInjector to apply to the namespaces selected by 'spec.namespaces' or 'spec.namespaceLabelSelector'. Required.")]
        public V1Beta1AgentInjector? Template { get; set; }

        [Description("The namespaces to apply this AgentInjector template to. Glob syntax is supported. Optional, defaults to none.")]
        public IReadOnlyCollection<string> Namespaces { get; set; } = Array.Empty<string>();

        [Description("The labels to select which namespaces to apply this AgentInjector template to. Optional, defaults to empty.")]
        public IReadOnlyCollection<ClusterNamespaceLabelSelectorSpec> NamespaceLabelSelector { get; set; } = Array.Empty<ClusterNamespaceLabelSelectorSpec>();
    }
}
