using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Features.Variance;
using Contrast.K8s.AgentOperator.Autofac;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Injecting;
using Contrast.K8s.AgentOperator.Core.State;
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
                if (_environment.IsDevelopment())
                {
                    settings.EnableLeaderElection = false;
                }
            });

            services.AddCertificateManager();
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

            // Options
            RegisterOptions(builder);

            // Workers
            builder.RegisterAssemblyTypes(assembly).PublicOnly().AssignableTo<BackgroundService>().As<IHostedService>();

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
        }

        private static void RegisterOptions(ContainerBuilder builder)
        {
            builder.Register(_ =>
            {
                // TODO Need to set POD_NAMESPACE.
                var @namespace = "default";
                if (Environment.GetEnvironmentVariable("POD_NAMESPACE") is { } podNamespace)
                {
                    @namespace = podNamespace.Trim();
                }

                return new OperatorOptions(@namespace);
            }).SingleInstance();

            builder.Register(_ =>
            {
                var dnsNames = new List<string>
                {
                    "localhost"
                };

                // TODO need to set in the form of: 
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
        }
    }
}
