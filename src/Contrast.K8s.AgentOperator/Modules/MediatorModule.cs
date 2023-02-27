// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Features.Variance;
using Contrast.K8s.AgentOperator.Autofac;
using JetBrains.Annotations;
using MediatR;
using System;

namespace Contrast.K8s.AgentOperator.Modules
{
    [UsedImplicitly]
    public class MediatorModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Mediator>()
                   .As<IMediator>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<MediatorServiceProvider>()
                   .As<IServiceProvider>()
                   .SingleInstance();
            builder.RegisterSource(new ContravariantRegistrationSource());
            builder.RegisterAssemblyTypes(ThisAssembly)
                   .PublicOnly()
                   .AssignableToOpenType(typeof(IRequestHandler<>))
                   .AsImplementedInterfaces()
                   .InstancePerLifetimeScope();
            builder.RegisterAssemblyTypes(ThisAssembly)
                   .PublicOnly()
                   .AssignableToOpenType(typeof(IRequestHandler<,>))
                   .AsImplementedInterfaces()
                   .InstancePerLifetimeScope();
            builder.RegisterAssemblyTypes(ThisAssembly)
                   .PublicOnly()
                   .AssignableToOpenType(typeof(INotificationHandler<>))
                   .AsImplementedInterfaces()
                   .InstancePerLifetimeScope();
        }

        private class MediatorServiceProvider : IServiceProvider
        {
            private readonly IComponentContext _context;

            public MediatorServiceProvider(IComponentContext context)
            {
                _context = context;
            }

            public object GetService(Type serviceType)
            {
                return _context.Resolve(serviceType);
            }
        }
    }
}
