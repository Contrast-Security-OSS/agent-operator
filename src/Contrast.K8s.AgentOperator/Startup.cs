// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Features.Variance;
using Contrast.K8s.AgentOperator.Autofac;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Leading;
using Contrast.K8s.AgentOperator.Core.Reactions;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.Telemetry;
using Contrast.K8s.AgentOperator.Core.Telemetry.Client;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using Contrast.K8s.AgentOperator.Core.Telemetry.Counters;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s;
using KubeOps.Operator;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contrast.K8s.AgentOperator
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddKubernetesOperator(settings =>
                    {
                        // We handle leadership ourselves.
                        settings.OnlyWatchEventsWhenLeader = false;
                    })
                    .AddReadinessCheck<ReadinessCheck>();
            services.AddCertificateManager();
            services.AddControllers();

            // Not actually used, but hot-reload uses it?
            services.AddRazorPages();
        }

        // ReSharper disable once UnusedMember.Global
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var assembly = typeof(Startup).Assembly;

            builder.ApplyContrastConventions(assembly);

            // These must be cached, as they parse PEM's on ctor.
            builder.Register(_ => KubernetesClientConfiguration.BuildDefaultConfig()).AsSelf().SingleInstance();
            builder.Register(x => new KubernetesClient(x.Resolve<KubernetesClientConfiguration>())).As<IKubernetesClient>().SingleInstance();
            builder.Register(x => x.Resolve<IKubernetesClient>().ApiClient).As<IKubernetes>().SingleInstance();

            builder.RegisterType<EventStream>().As<IEventStream>().SingleInstance();
            builder.RegisterType<StateContainer>().As<IStateContainer>().SingleInstance();
            builder.RegisterType<GlobMatcher>().As<IGlobMatcher>().SingleInstance();
            builder.RegisterType<KestrelCertificateSelector>().As<IKestrelCertificateSelector>().SingleInstance();
            builder.RegisterType<LeaderElectionState>().As<ILeaderElectionState>().SingleInstance();

            builder.RegisterType<ClusterIdState>().As<IClusterIdState>().SingleInstance();
            builder.RegisterType<TelemetryState>().AsSelf().SingleInstance();
            builder.Register(_ => new TelemetryState(OperatorVersion.Version)).AsSelf().SingleInstance();

            RegisterOptions(builder);
            builder.RegisterAssemblyTypes(assembly).PublicOnly().AssignableTo<BackgroundService>().As<IHostedService>();
            builder.RegisterAssemblyTypes(assembly).PublicOnly().AssignableTo<IAgentPatcher>().As<IAgentPatcher>();

            // Telemetry
            builder.Register(x => x.Resolve<ITelemetryClientFactory>().Create()).As<ITelemetryClient>().SingleInstance();
            builder.RegisterType<PerformanceCounterContainer>().AsSelf().SingleInstance();

            // MediatR
            builder.RegisterType<Mediator>()
                   .As<IMediator>()
                   .InstancePerLifetimeScope();
            builder.Register<ServiceFactory>(context =>
            {
                var c = context.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });
            builder.RegisterSource(new ContravariantRegistrationSource());
            builder.RegisterAssemblyTypes(assembly)
                   .PublicOnly()
                   .AssignableToOpenType(typeof(IRequestHandler<>))
                   .AsImplementedInterfaces()
                   .InstancePerDependency();
            builder.RegisterAssemblyTypes(assembly)
                   .PublicOnly()
                   .AssignableToOpenType(typeof(IRequestHandler<,>))
                   .AsImplementedInterfaces()
                   .InstancePerDependency();
            builder.RegisterAssemblyTypes(assembly)
                   .PublicOnly()
                   .AssignableToOpenType(typeof(INotificationHandler<>))
                   .AsImplementedInterfaces()
                   .InstancePerDependency();
        }

        public void Configure(IApplicationBuilder app)
        {
            // If needed:
            //var container = app.ApplicationServices.GetAutofacRoot();

            if (_environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseKubernetesOperator();

            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        private static void RegisterOptions(ContainerBuilder builder)
        {
            builder.Register(_ =>
            {
                var @namespace = "default";
                if (Environment.GetEnvironmentVariable("POD_NAMESPACE") is { } podNamespace)
                {
                    @namespace = podNamespace.Trim();
                }

                var settleDuration = 10;
                if (Environment.GetEnvironmentVariable("CONTRAST_SETTLE_DURATION") is { } settleDurationStr
                    && int.TryParse(settleDurationStr, out var parsedSettleDuration)
                    && parsedSettleDuration > -1)
                {
                    settleDuration = parsedSettleDuration;
                }

                return new OperatorOptions(@namespace, SettlingDurationSeconds: settleDuration);
            }).SingleInstance();

            builder.Register(_ =>
            {
                // TODO Need to set this for public releases.
                if (Environment.GetEnvironmentVariable("CONTRAST_DEFAULT_REGISTRY")
                    is { } defaultRegistry)
                {
                    return new ImageRepositoryOptions(defaultRegistry);
                }

                throw new NotImplementedException("No default registry was set.");
            }).SingleInstance();

            builder.Register(_ =>
            {
                var dnsNames = new List<string>
                {
                    "localhost"
                };

                // ingress-nginx-controller-admission,ingress-nginx-controller-admission.$(POD_NAMESPACE).svc
                if (Environment.GetEnvironmentVariable("CONTRAST_WEBHOOK_HOSTS") is { } webHookHosts)
                {
                    dnsNames.AddRange(webHookHosts.Split(",", StringSplitOptions.RemoveEmptyEntries));
                }

                return new TlsCertificateOptions("contrast-web-hook", dnsNames, TimeSpan.FromDays(365 * 100));
            }).SingleInstance();

            builder.Register(x =>
            {
                var webHookSecret = "contrast-web-hook-secret";
                if (Environment.GetEnvironmentVariable("CONTRAST_WEBHOOK_SECRET") is { } customWebHookSecret)
                {
                    webHookSecret = customWebHookSecret.Trim();
                }

                var @namespace = x.Resolve<OperatorOptions>().Namespace;

                return new TlsStorageOptions(webHookSecret, @namespace);
            }).SingleInstance();

            builder.Register(_ =>
            {
                var webHookConfigurationName = "contrast-web-hook-configuration";
                if (Environment.GetEnvironmentVariable("CONTRAST_WEBHOOK_CONFIGURATION") is { } customWebHookSecret)
                {
                    webHookConfigurationName = customWebHookSecret.Trim();
                }

                return new MutatingWebHookOptions(webHookConfigurationName);
            }).SingleInstance();

            builder.Register(x =>
            {
                var @namespace = x.Resolve<OperatorOptions>().Namespace;
                return new TelemetryOptions("contrast-cluster-id", @namespace);
            }).SingleInstance();
        }
    }
}
