// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using JetBrains.Annotations;
using k8s;
using KubeOps.KubernetesClient;

namespace Contrast.K8s.AgentOperator.Modules;

[UsedImplicitly]
public class KubeOpsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // These must be cached, as they parse PEM's on ctor.
        builder.Register(_ => KubernetesClientConfiguration.BuildDefaultConfig()).AsSelf().SingleInstance();
        builder.Register(x => new KubernetesClient(x.Resolve<KubernetesClientConfiguration>())).As<IKubernetesClient>().SingleInstance();
        builder.Register(x => x.Resolve<IKubernetesClient>().ApiClient).As<IKubernetes>().SingleInstance();
    }
}
