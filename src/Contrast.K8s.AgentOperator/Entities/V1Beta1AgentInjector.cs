// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;
using KubeOps.Abstractions.Rbac;

namespace Contrast.K8s.AgentOperator.Entities;

[KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "AgentInjector", PluralName = "agentinjectors")]
[EntityRbac(typeof(V1Beta1AgentInjector), Verbs = VerbConstants.FullControl)]
public partial class V1Beta1AgentInjector : CustomKubernetesEntity<V1Beta1AgentInjector.AgentInjectorSpec>
{
    public class AgentInjectorSpec
    {
        [Description("Enable this agent injector. Defaults to 'true'.")]
        public bool Enabled { get; set; } = true;

        [Pattern(RegexConstants.InjectorVersionRegex)]
        [Description("The version of the agent to inject. The literal 'latest' will inject the latest version. Partial version matches are supported, e.g. '2' will select the version '2.1.0'. Defaults to 'latest'.")]
        public string? Version { get; set; }

        [Required, Pattern(RegexConstants.AgentTypeRegex)]
        [Description("The type of agent to inject. Can be one of ['dotnet-core', 'java', 'nodejs', 'nodejs-esm', 'php', 'python']. Required.")]
        public string Type { get; set; } = null!;

        [Description("Overrides the default agent images.")]
        public AgentInjectorImageSpec Image { get; set; } = new();

        [Description("Select which Deployment/StatefulSet/DaemonSet/Rollout pods are eligible for agent injection. Under OpenShift, DeploymentConfig is also supported.")]
        public AgentInjectorSelectorSpec Selector { get; set; } = new();

        [Description("The connection the injected agent will use to communicate with Contrast.")]
        public AgentInjectorConnectionSpec? Connection { get; set; } = new();

        [Description("The configuration the injected agent will use.")]
        public AgentInjectorConfigurationSpec? Configuration { get; set; } = new();
    }

    public class AgentInjectorImageSpec
    {
        [Description("The fully qualified name of the registry to pull agent images from. This registry must be accessible by the pods being injected and the operator. Defaults to the official Contrast container image registry.")]
        public string? Registry { get; set; }

        [Description("The name of the injector image to use. The default depends on the value of spec.type.")]
        public string? Name { get; set; }

        [Description("The name of a pull secret to append to the pod's imagePullSecrets list.")]
        public string? PullSecretName { get; set; }

        [Pattern(RegexConstants.PullPolicyRegex)]
        [Description("The pull policy to use when fetching Contrast images. See Kubernetes imagePullPolicy for more information. Defaults to 'Always'.")]
        public string? PullPolicy { get; set; } = "Always";
    }

    public class AgentInjectorSelectorSpec
    {
        [Description("Container images to inject the agent into. Glob patterns are supported. If empty (the default), selects all containers in Pod.")]
        public IReadOnlyCollection<string> Images { get; set; } = Array.Empty<string>();

        [Description("Deployment/StatefulSet/DaemonSet/DeploymentConfig labels whose pods are eligible for agent injection. If empty (the default), selects all workloads in namespace.")]
        public IReadOnlyCollection<LabelSelectorSpec> Labels { get; set; } = Array.Empty<LabelSelectorSpec>();
    }

    public class LabelSelectorSpec
    {
        [Required]
        [Description("The name of the label to match. Required.")]
        public string Name { get; set; } = null!;

        [Required]
        [Description("The value of the label to match. Glob patterns are supported. Required.")]
        public string Value { get; set; } = null!;
    }

    public class AgentInjectorConnectionSpec
    {
        [Description("The name of AgentConnection resource. Must exist within the same namespace. Defaults to the AgentConnection specified by a ClusterAgentConnection.")]
        public string? Name { get; set; }
    }

    public class AgentInjectorConfigurationSpec
    {
        [Description("The name of a AgentConfiguration resource. Must exist within the same namespace. Defaults to the AgentConfiguration specified by a ClusterAgentConfiguration.")]
        public string? Name { get; set; }
    }
}
