using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public static class StateContainerExtensions
    {
        public static async ValueTask<AgentInjectorResource?> GetReadyAgentInjectorById(this IStateContainer state,
                                                                                        string name,
                                                                                        string @namespace,
                                                                                        CancellationToken cancellationToken = default)
        {
            var injector = await state.GetById<AgentInjectorResource>(name, @namespace, cancellationToken);
            if (injector is { Enabled: true })
            {
                var configurationReferenceFound = injector.ConfigurationReference is not { } configurationRef
                                                  || await state.ExistsById<AgentConfigurationResource>(
                                                      configurationRef.Name,
                                                      configurationRef.Namespace,
                                                      cancellationToken
                                                  );

                var connectionResourceFound = injector.ConnectionReference is { } connectionRef
                                              && await state.GetReadyAgentConnectionById(
                                                  connectionRef.Name,
                                                  connectionRef.Namespace,
                                                  cancellationToken
                                              ) != null;

                var pullSecretFound = injector.ImagePullSecret == null
                                      || await state.HasSecretKey(injector.ImagePullSecret, cancellationToken);

                if (configurationReferenceFound && connectionResourceFound && pullSecretFound)
                {
                    return injector;
                }
            }

            return null;
        }

        public static async ValueTask<AgentConnectionResource?> GetReadyAgentConnectionById(this IStateContainer state,
                                                                                            string name,
                                                                                            string @namespace,
                                                                                            CancellationToken cancellationToken = default)
        {
            var connection = await state.GetById<AgentConnectionResource>(name, @namespace, cancellationToken);
            if (connection != null)
            {
                var hasApiKeySecret = await state.HasSecretKey(connection.ApiKey, cancellationToken);
                var hasServiceKeySecret = await state.HasSecretKey(connection.ServiceKey, cancellationToken);
                var hasUserNameSecret = await state.HasSecretKey(connection.UserName, cancellationToken);
                if (hasApiKeySecret
                    && hasServiceKeySecret
                    && hasUserNameSecret)
                {
                    return connection;
                }
            }

            return null;
        }

        private static async Task<bool> HasSecretKey(this IStateContainer state,
                                                     SecretReference secretRef,
                                                     CancellationToken cancellationToken = default)
        {
            return await state.GetById<SecretResource>(secretRef.Name, secretRef.Namespace, cancellationToken) is { } secret
                   && secret.Keys.Contains(secretRef.Key);
        }

        private static async Task<SecretResource?> GetSecret(this IStateContainer state,
                                                             SecretReference secretRef,
                                                             CancellationToken cancellationToken = default)
        {
            return await state.GetById<SecretResource>(secretRef.Name, secretRef.Namespace, cancellationToken);
        }

        public static async ValueTask<InjectorBundle?> GetInjectorBundle(this IStateContainer state,
                                                                         string injectorName,
                                                                         string injectorNamespace,
                                                                         CancellationToken cancellationToken = default)
        {
            if (await state.GetById<AgentInjectorResource>(injectorName, injectorNamespace, cancellationToken)
                    is { ConnectionReference: { } connectionRef } injector
                && await state.GetById<AgentConnectionResource>(connectionRef.Name, connectionRef.Namespace, cancellationToken)
                    is { } connection
                && await state.GetSecret(connection.UserName, cancellationToken)
                    is { } userNameSecret
                && await state.GetSecret(connection.ServiceKey, cancellationToken)
                    is { } serviceKeySecret
                && await state.GetSecret(connection.ApiKey, cancellationToken)
                    is { } apiKeySecret
               )
            {
                var configuration = injector.ConfigurationReference is { } configurationRef
                    ? await state.GetById<AgentConfigurationResource>(configurationRef.Name, configurationRef.Namespace, cancellationToken)
                    : null;

                var imagePullSecret = injector.ImagePullSecret is { } imagePullSecretRef
                    ? await state.GetSecret(imagePullSecretRef, cancellationToken)
                    : null;

                var secrets = new List<SecretResource>
                {
                    userNameSecret,
                    serviceKeySecret,
                    apiKeySecret
                };

                if (imagePullSecret != null)
                {
                    secrets.Add(imagePullSecret);
                }

                return new InjectorBundle(injector, connection, configuration, secrets);
            }

            return null;
        }
    }

    public record InjectorBundle(AgentInjectorResource Injector,
                                 AgentConnectionResource Connection,
                                 AgentConfigurationResource? Configuration,
                                 IReadOnlyCollection<SecretResource> Secrets);
}
