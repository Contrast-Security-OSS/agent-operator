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
        /// <summary>
        /// Is this agent injector enabled.
        /// Defaults to 'true'.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The version of the agent to inject. The literal 'latest' will inject the latest version. Partial version matches are supported, e.g. '2' will select the version '2.1.0'.
        /// Defaults to 'latest'.
        /// </summary>
        [Pattern(RegexConstants.InjectorVersionRegex)]
        public string? Version { get; set; }

        /// <summary>
        /// The type of agent to inject. Can be one of ['dotnet-core', 'java', 'nodejs', 'nodejs-esm', 'php', 'python'].
        /// Required.
        /// </summary>
        [Required, Pattern(RegexConstants.AgentTypeRegex)]
        public string Type { get; set; } = null!;

        /// <summary>
        /// Overrides the default agent images.
        /// </summary>
        public AgentInjectorImageSpec Image { get; set; } = new();

        /// <summary>
        /// Select which Deployment/StatefulSet/DaemonSet pods are eligible for agent injection.
        /// Under OpenShift, DeploymentConfig is also supported.
        /// </summary>
        public AgentInjectorSelectorSpec Selector { get; set; } = new();

        /// <summary>
        /// The connection the injected agent will use to communicate with Contrast.
        /// </summary>
        public AgentInjectorConnectionSpec? Connection { get; set; } = new();

        /// <summary>
        /// The configuration the injected agent will use.
        /// </summary>
        public AgentInjectorConfigurationSpec? Configuration { get; set; } = new();
    }

    public class AgentInjectorImageSpec
    {
        /// <summary>
        /// The fully qualified name of the registry to pull agent images from. This registry must be accessible by the pods being injected and the operator.
        /// Defaults to the official Contrast container image registry.
        /// </summary>
        public string? Registry { get; set; }

        /// <summary>
        /// The name of the injector image to use.
        /// The default depends on the value of spec.type.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The name of a pull secret to append to the pod's imagePullSecrets list.
        /// </summary>
        public string? PullSecretName { get; set; }

        /// <summary>
        /// The pull policy to use when fetching Contrast images. See Kubernetes imagePullPolicy for more information.
        /// Defaults to "Always".
        /// </summary>
        [Pattern(RegexConstants.PullPolicyRegex)]
        public string? PullPolicy { get; set; } = "Always";
    }

    public class AgentInjectorSelectorSpec
    {
        /// <summary>
        /// Container images to inject the agent into. Glob patterns are supported.
        /// If empty (the default), selects all containers in Pod.
        /// </summary>
        public IReadOnlyCollection<string> Images { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Deployment/StatefulSet/DaemonSet/DeploymentConfig labels whose pods are eligible for agent injection.
        /// If empty (the default), selects all workloads in namespace.
        /// </summary>
        public IReadOnlyCollection<LabelSelectorSpec> Labels { get; set; } = Array.Empty<LabelSelectorSpec>();
    }

    public class LabelSelectorSpec
    {
        /// <summary>
        /// The name of the label to match.
        /// Required.
        /// </summary>
        [Required]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The value of the label to match. Glob patterns are supported.
        /// Required.
        /// </summary>
        [Required]
        public string Value { get; set; } = null!;
    }

    public class AgentInjectorConnectionSpec
    {
        /// <summary>
        /// The name of AgentConnection resource. Must exist within the same namespace.
        /// Defaults to the AgentConnection specified by a ClusterAgentConnection.
        /// </summary>
        public string? Name { get; set; }
    }

    public class AgentInjectorConfigurationSpec
    {
        /// <summary>
        /// The name of a AgentConfiguration resource. Must exist within the same namespace.
        /// Defaults to the AgentConfiguration specified by a ClusterAgentConfiguration.
        /// </summary>
        public string? Name { get; set; }
    }
}
