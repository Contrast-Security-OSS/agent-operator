using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Features.ResolveAnything;
using Autofac.Features.Scanning;

namespace Contrast.K8s.AgentOperator.Autofac
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder ApplyContrastConventions(this ContainerBuilder builder, Assembly executingAssembly)
        {
            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            builder.RegisterAssemblyTypes(executingAssembly).WithDefaultInterfaces();

            return builder;
        }

        public static IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> WithDefaultInterfaces
            <TLimit, TScanningActivatorData, TRegistrationStyle>(this IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> registration)
            where TScanningActivatorData : ScanningActivatorData
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            return registration.PublicOnly()
                               .Where(x => GetDefaultInterface(x) != null)
                               .As(x => GetDefaultInterface(x)!);
        }

        public static IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> AssignableToOpenType
            <TLimit, TScanningActivatorData, TRegistrationStyle>(this IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> registration,
                                                                 Type openType)
            where TScanningActivatorData : ScanningActivatorData
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            return registration.Where(x => x.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == openType));
        }

        private static Type? GetDefaultInterface(Type type)
        {
            var defaultInterfaceName = $"I{type.Name}";
            return type.GetInterfaces().SingleOrDefault(x => string.Equals(x.Name, defaultInterfaceName, StringComparison.Ordinal));
        }
    }
}
