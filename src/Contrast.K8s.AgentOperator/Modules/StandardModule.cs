// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.Leading;
using Contrast.K8s.AgentOperator.Core.Reactions;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.Reactions.Merging;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.Telemetry;
using Contrast.K8s.AgentOperator.Core.Telemetry.Client;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using Contrast.K8s.AgentOperator.Core.Telemetry.Counters;
using Contrast.K8s.AgentOperator.Core.Tls;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Contrast.K8s.AgentOperator.Modules
{
    [UsedImplicitly]
    public class StandardModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<EventStream>().As<IEventStream>().SingleInstance();
            builder.RegisterType<StateContainer>().As<IStateContainer>().SingleInstance();
            builder.RegisterType<GlobMatcher>().As<IGlobMatcher>().SingleInstance();
            builder.RegisterType<KestrelCertificateSelector>().As<IKestrelCertificateSelector>().SingleInstance();
            builder.RegisterType<LeaderElectionState>().As<ILeaderElectionState>().SingleInstance();
            builder.RegisterType<MergingStateProvider>().AsSelf().SingleInstance();

            builder.RegisterType<ClusterIdState>().As<IClusterIdState>().SingleInstance();
            builder.RegisterType<TelemetryState>().AsSelf().SingleInstance();
            builder.Register(_ => new TelemetryState(OperatorVersion.Version)).AsSelf().SingleInstance();

            // ResourceComparer needs to cache.
            builder.RegisterType<ResourceComparer>().As<IResourceComparer>().SingleInstance();

            builder.RegisterAssemblyTypes(ThisAssembly).PublicOnly().AssignableTo<BackgroundService>().As<IHostedService>();
            builder.RegisterAssemblyTypes(ThisAssembly).PublicOnly().AssignableTo<IAgentPatcher>().As<IAgentPatcher>();

            // Telemetry
            builder.Register(x => x.Resolve<ITelemetryClientFactory>().Create()).As<ITelemetryClient>().SingleInstance();
            builder.RegisterType<PerformanceCounterContainer>().AsSelf().SingleInstance();
        }
    }
}
