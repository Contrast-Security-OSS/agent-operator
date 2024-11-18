// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Rbac;

namespace Contrast.K8s.AgentOperator.Entities.OpenShift;

[KubernetesEntity(Group = "apps.openshift.io", ApiVersion = "v1", Kind = "DeploymentConfig", PluralName = "deploymentconfigs")]
[EntityRbac(typeof(V1DeploymentConfig), Verbs = VerbConstants.ReadAndPatch)]
public partial class V1DeploymentConfig : CustomKubernetesEntity<V1DeploymentConfig.DeploymentConfigSpec>
{
    /// <summary>
    /// DeploymentConfigSpec represents the desired state of the deployment.
    /// </summary>
    public class DeploymentConfigSpec
    {
        /// <summary>
        /// Selector is a label query over pods that should match the Replicas count.
        /// </summary>
        [JsonPropertyName("selector")]
        public IDictionary<string, string> Selector { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Template is the object that describes the pod that will be created if insufficient replicas are detected.
        /// </summary>
        [JsonPropertyName("template")]
        public V1PodTemplateSpec? Template { get; set; }
    }
}
