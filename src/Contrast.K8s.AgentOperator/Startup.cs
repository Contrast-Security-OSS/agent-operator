using Autofac;
using Contrast.K8s.AgentOperator.Autofac;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.State;
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
        }

        // ReSharper disable once UnusedMember.Global
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var assembly = typeof(Startup).Assembly;

            builder.ApplyContrastConventions(assembly);

            builder.Register(_ => KubernetesClientConfiguration.BuildDefaultConfig()).AsSelf().SingleInstance();
            builder.Register(x => new KubernetesClient(x.Resolve<KubernetesClientConfiguration>())).As<IKubernetesClient>();
            builder.Register(x => x.Resolve<IKubernetesClient>().ApiClient).As<IKubernetes>();

            builder.RegisterType<EventStream>().As<IEventStream>().SingleInstance();
            builder.RegisterType<StateContainer>().As<IStateContainer>().SingleInstance();

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
            //builder.RegisterSource(new ContravariantRegistrationSource());
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

            //builder.RegisterBuildCallback(c =>
            //{
            //    var container = (IContainer)c;
            //    var tracer = new DefaultDiagnosticTracer();
            //    tracer.OperationCompleted += (sender, args) => { Console.WriteLine(args.TraceContent); };
            //    container.SubscribeToDiagnostics(tracer);
            //});
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
    }
}
