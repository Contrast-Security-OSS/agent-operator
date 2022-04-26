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
            if (injector != null)
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

                if (configurationReferenceFound && connectionResourceFound)
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
    }
}
