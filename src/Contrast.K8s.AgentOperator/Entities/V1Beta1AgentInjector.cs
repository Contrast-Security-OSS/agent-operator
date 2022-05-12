using System;
using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Entities
{
    [KubernetesEntity(Group = "agents.contrastsecurity.com", ApiVersion = "v1beta1", Kind = "AgentInjector", PluralName = "agentinjectors")]
    [EntityRbac(typeof(V1Beta1AgentInjector), Verbs = VerbConstants.ReadOnly)]
    public class V1Beta1AgentInjector : CustomKubernetesEntity<V1Beta1AgentInjector.AgentInjectorSpec>
    {
        public class AgentInjectorSpec
        {
            /// <summary>
            /// Is this agent injector enabled.
            /// Defaults to 'true'.
            /// </summary>
            public bool Enabled { get; set; } = true;

            /// <summary>
            /// The version of the agent to inject. The literal 'latest' will inject the latest version detected.
            /// Defaults to 'latest'.
            /// </summary>
            [Pattern(RegexConstants.InjectorVersionRegex)]
            public string? Version { get; set; }

            /// <summary>
            /// The type of agent to inject. Can be one of ['dotnet-core', 'java', 'nodejs'].
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
            /// </summary>
            public AgentInjectorSelectorSpec Selector { get; set; } = new();

            /// <summary>
            /// The connection the injected agent will use to communicate with Contrast.
            /// </summary>
            [Required]
            public AgentInjectorConnectionSpec Connection { get; set; } = new();

            /// <summary>
            /// The configuration the injected agent will use.
            /// </summary>
            public AgentInjectorConfigurationSpec? Configuration { get; set; } = new();
        }

        public class AgentInjectorImageSpec
        {
            /// <summary>
            /// The fully qualified name of the repository to pull agent images from. This repository must be accessible by the pods being injected.
            /// Defaults to the official Contrast container image repositories.
            /// </summary>
            public string? Repository { get; set; }

            /// <summary>
            /// The name of the injector image to use.
            /// The default depends on the value of spec.type.
            /// </summary>
            public string? Name { get; set; }

            /// <summary>
            /// The name of a pull secret to append to the pod's imagePullSecrets.
            /// </summary>
            public string? PullSecretName { get; set; }
        }

        public class AgentInjectorSelectorSpec
        {
            /// <summary>
            /// Container images to inject the agent into. Glob patterns are supported.
            /// Defaults to ['*'].
            /// </summary>
            public IReadOnlyCollection<string> Images { get; set; } = Array.Empty<string>();

            /// <summary>
            /// Deployment/StatefulSet/DaemonSet labels whose pods are eligible for agent injection.
            /// Defaults to selecting everything.
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
            /// </summary>
            [Required]
            public string Value { get; set; } = null!;
        }

        public class AgentInjectorConnectionSpec
        {
            /// <summary>
            /// The name of AgentConnection resource. Must exist within the same namespace.
            /// Required.
            /// </summary>
            [Required]
            public string Name { get; set; } = null!;
        }

        public class AgentInjectorConfigurationSpec
        {
            /// <summary>
            /// The name of a AgentConfiguration resource. Must exist within the same namespace.
            /// </summary>
            public string? Name { get; set; }
        }
    }
}
