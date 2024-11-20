// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;
using KubeOps.Abstractions.Rbac;

namespace Contrast.K8s.AgentOperator.Entities.OpenShift;

[Ignore] //Don't generate a CRD for this
[KubernetesEntity(Group = "apps.openshift.io", ApiVersion = "v1", Kind = "DeploymentConfig", PluralName = "deploymentconfigs")]
[EntityRbac(typeof(V1DeploymentConfig), Verbs = VerbConstants.ReadAndPatch)]
public partial class V1DeploymentConfig : CustomKubernetesEntity<V1DeploymentConfig.DeploymentConfigSpec>
{
    public class DeploymentConfigSpec
    {
        [JsonPropertyName("selector")]
        public IDictionary<string, string> Selector { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("template")]
        public V1PodTemplateSpec? Template { get; set; }
    }
}
