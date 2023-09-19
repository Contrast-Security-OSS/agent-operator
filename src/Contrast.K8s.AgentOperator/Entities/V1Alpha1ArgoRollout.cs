// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using k8s;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Entities;
using Contrast.K8s.AgentOperator.Core.Kube;
using JetBrains.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Entities
{



    [IgnoreEntity]
    [EntityRbac(typeof(V1alpha1Rollout), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    [KubernetesEntity(Group = "argoproj.io", ApiVersion = "v1alpha1", Kind = "Rollout", PluralName = "rollouts")]
    public class V1alpha1Rollout : CustomKubernetesEntity<V1alpha1Rollout.V1alpha1rolloutSpec>
    {
        public class V1alpha1rolloutSpec : V1DeploymentSpec
        {
            //public string Host { get; set; }
        }
    }

}
