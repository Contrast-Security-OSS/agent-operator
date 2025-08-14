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
        public AgentInjectorTemplate? Template { get; set; }

        [Description("The namespaces to apply this AgentInjector template to. Glob syntax is supported. Optional, defaults to none.")]
        public IReadOnlyCollection<string> Namespaces { get; set; } = Array.Empty<string>();

        [Description("The labels to select which namespaces to apply this AgentInjector template to. Optional, defaults to empty.")]
        public IReadOnlyCollection<ClusterNamespaceLabelSelectorSpec> NamespaceLabelSelector { get; set; } = Array.Empty<ClusterNamespaceLabelSelectorSpec>();
    }

    public class AgentInjectorTemplate
    {
        public AgentInjectorTemplateSpec Spec { get; set; } = new();
    }

    public class AgentInjectorTemplateSpec
    {
        [Description("Enable this agent injector. Defaults to 'true'.")]
        public bool Enabled { get; set; } = true;

        [Pattern(RegexConstants.InjectorVersionRegex)]
        [Description("The version of the agent to inject. The literal 'latest' will inject the latest version. Partial version matches are supported, e.g. '2' will select the version '2.1.0'. Defaults to 'latest'.")]
        public string? Version { get; set; }

        [Required, Pattern(RegexConstants.AgentTypeRegex)]
        [Description("The type of agent to inject. Can be one of ['dotnet-core', 'java', 'nodejs', 'nodejs-legacy', 'php', 'python', 'flex']. Required.")]
        public string Type { get; set; } = null!;

        [Description("Overrides the default agent images.")]
        public V1Beta1AgentInjector.AgentInjectorImageSpec Image { get; set; } = new();

        [Description("Select which Deployment/StatefulSet/DaemonSet/Rollout pods are eligible for agent injection. Under OpenShift, DeploymentConfig is also supported.")]
        public V1Beta1AgentInjector.AgentInjectorSelectorSpec Selector { get; set; } = new();
    }
}
