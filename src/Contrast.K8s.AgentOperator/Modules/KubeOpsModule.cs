// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Contrast.K8s.AgentOperator.Core.Extensions;
using DotnetKubernetesClient;
using JetBrains.Annotations;
using k8s;
using KubeOps.Operator.Caching;

namespace Contrast.K8s.AgentOperator.Modules
{
    [UsedImplicitly]
    public class KubeOpsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Disable KubeOps cache, we will use our own.
            // This has the side effect of breaking status modified events, which is fine for us.
            builder.RegisterGenericDecorator(typeof(NoOpResourceCacheDecorator<>), typeof(IResourceCache<>));

            // These must be cached, as they parse PEM's on ctor.
            builder.Register(_ => KubernetesClientConfiguration.BuildDefaultConfig()).AsSelf().SingleInstance();
            builder.Register(x => new KubernetesClient(x.Resolve<KubernetesClientConfiguration>())).As<IKubernetesClient>().SingleInstance();
            builder.Register(x => x.Resolve<IKubernetesClient>().ApiClient).As<IKubernetes>().SingleInstance();
        }
    }
}
