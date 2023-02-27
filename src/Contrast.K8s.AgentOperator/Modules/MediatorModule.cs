// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Features.Variance;
using Contrast.K8s.AgentOperator.Autofac;
using JetBrains.Annotations;
using MediatR;

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
    }
}
