// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using k8s.Models;
using k8s;
using KubeOps.Abstractions.Builder;
using Microsoft.Extensions.DependencyInjection;
using Contrast.K8s.AgentOperator.Entities.Argo;
using Contrast.K8s.AgentOperator.Entities.Dynatrace;
using Contrast.K8s.AgentOperator.Entities.OpenShift;
using Contrast.K8s.AgentOperator.Entities;

namespace Contrast.K8s.AgentOperator.Extensions
{
    public static class OperatorBuilderExtensions
    {
        public static IOperatorBuilder RegisterEntities(this IOperatorBuilder builder)
        {
            //All entities we want to watch
            builder.RegisterEntity<V1Beta1AgentConfiguration>();
            builder.RegisterEntity<V1Beta1AgentConfiguration>();
            builder.RegisterEntity<V1Beta1AgentConnection>();
            builder.RegisterEntity<V1Beta1AgentInjector>();
            builder.RegisterEntity<V1Beta1ClusterAgentConfiguration>();
            builder.RegisterEntity<V1Beta1ClusterAgentConnection>();
            builder.RegisterEntity<V1Beta1ClusterAgentInjector>();
            builder.RegisterEntity<V1DaemonSet>();
            builder.RegisterEntity<V1DeploymentConfig>();
            builder.RegisterEntity<V1Deployment>();
            builder.RegisterEntity<V1Beta1DynaKube>();
            builder.RegisterEntity<V1Pod>();
            builder.RegisterEntity<V1Alpha1Rollout>();
            builder.RegisterEntity<V1Secret>();
            builder.RegisterEntity<V1StatefulSet>();
            return builder;
        }

        public static IOperatorBuilder RegisterEntity<T>(this IOperatorBuilder builder) where T : IKubernetesObject<V1ObjectMeta>
        {
            builder.Services.AddHostedService<CustomResourceWatcher<T>>();
            return builder;
        }
    }
}
