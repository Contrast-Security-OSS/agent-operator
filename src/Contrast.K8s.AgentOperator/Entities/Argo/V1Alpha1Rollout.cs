// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Entities;

namespace Contrast.K8s.AgentOperator.Entities.Argo;

[KubernetesEntity(Group = "argoproj.io", ApiVersion = "v1alpha1", Kind = "Rollout", PluralName = "rollouts"), UsedImplicitly]
public class V1Alpha1Rollout : CustomKubernetesEntity<V1Alpha1Rollout.RolloutSpec>
{
    //Drop-in replacement for Deployment (with additional fields for the rollout info)
    public class RolloutSpec : V1DeploymentSpec
    {
    }
}
