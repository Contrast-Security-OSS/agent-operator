// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
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
        public static async ValueTask<ReadyResult<AgentInjectorResource>> GetReadyAgentInjectorById(this IStateContainer state,
                                                                                                    string name,
                                                                                                    string @namespace,
                                                                                                    CancellationToken cancellationToken = default)
        {
            var context = new ReadyContext();

            var injector = await state.GetById<AgentInjectorResource>(name, @namespace, cancellationToken);
            if (injector == null)
            {
                context.AddFailureReason($"Injector '{@namespace}/{name}' appears to have disappeared.");
            }
            else if (!injector.Enabled)
            {
                context.AddFailureReason($"Injector '{@namespace}/{name}' is disabled and will be ignored.");
            }
            else
            {
                var connectionResourceFound = injector.ConnectionReference is { } connectionRef
                                              && await state.GetReadyAgentConnectionById(
                                                  connectionRef.Name,
                                                  connectionRef.Namespace,
                                                  context,
                                                  cancellationToken
                                              ) != null;

                var pullSecretFound = injector.ImagePullSecret == null
                                      || await state.HasSecretKey(injector.ImagePullSecret, cancellationToken);
                if (!pullSecretFound
                    && injector.ImagePullSecret is { } secret)
                {
                    context.AddFailureReason($"Pull secret was set, but Secret '{secret.Namespace}/{secret.Name}' with key '{secret.Key}' could not be found.");
                }

                if (connectionResourceFound && pullSecretFound)
                {
                    return new IsReadyResult<AgentInjectorResource>(injector);
                }
            }

            return new NotReadyResult<AgentInjectorResource>(context.Reasons);
        }

        private static async ValueTask<AgentConnectionResource?> GetReadyAgentConnectionById(this IStateContainer state,
                                                                                             string name,
                                                                                             string @namespace,
                                                                                             ReadyContext readyContext,
                                                                                             CancellationToken cancellationToken = default)
        {
            var connection = await state.GetById<AgentConnectionResource>(name, @namespace, cancellationToken);
            if (connection != null)
            {
                var hasApiKeySecret = await state.HasSecretKey(connection.ApiKey, cancellationToken);
                if (!hasApiKeySecret)
                {
                    readyContext.AddFailureReason(
                        $"Secret '{connection.ApiKey.Namespace}/{connection.ApiKey.Name}' with key '{connection.ApiKey.Key}' could not be found.");
                }

                var hasServiceKeySecret = await state.HasSecretKey(connection.ServiceKey, cancellationToken);
                if (!hasServiceKeySecret)
                {
                    readyContext.AddFailureReason(
                        $"Secret '{connection.ServiceKey.Namespace}/{connection.ServiceKey.Name}' with key '{connection.ServiceKey.Key}' could not be found.");
                }

                var hasUserNameSecret = await state.HasSecretKey(connection.UserName, cancellationToken);
                if (!hasUserNameSecret)
                {
                    readyContext.AddFailureReason(
                        $"Secret '{connection.UserName.Namespace}/{connection.UserName.Name}' with key '{connection.UserName.Key}' could not be found.");
                }

                if (hasApiKeySecret
                    && hasServiceKeySecret
                    && hasUserNameSecret)
                {
                    return connection;
                }
            }
            else
            {
                readyContext.AddFailureReason($"AgentConnection '{@namespace}/{name}' could not be found.");
            }

            return null;
        }

        private static async ValueTask<bool> HasSecretKey(this IStateContainer state,
                                                          SecretReference secretRef,
                                                          CancellationToken cancellationToken = default)
        {
            return await state.GetById<SecretResource>(secretRef.Name, secretRef.Namespace, cancellationToken) is { } secret
                   && secret.KeyPairs.Any(x => string.Equals(x.Key, secretRef.Key, StringComparison.OrdinalIgnoreCase));
        }

        private static async ValueTask<SecretResource?> GetSecret(this IStateContainer state,
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

        private class ReadyContext
        {
            private readonly List<string> _reasons = new();

            public IReadOnlyCollection<string> Reasons => _reasons;

            public bool HasFailures => _reasons.Count != 0;

            public void AddFailureReason(string reason)
            {
                _reasons.Add(reason);
            }
        }
    }

    public record InjectorBundle(AgentInjectorResource Injector,
                                 AgentConnectionResource Connection,
                                 AgentConfigurationResource? Configuration,
                                 IReadOnlyCollection<SecretResource> Secrets);

    public abstract record ReadyResult<T>;

    public record IsReadyResult<T>(T Data) : ReadyResult<T>;

    public record NotReadyResult<T>(IReadOnlyCollection<string> FailureReasons) : ReadyResult<T>
    {
        public string FormatFailureReasons()
        {
            var quotedReasons = FailureReasons.Select(x => $"'{x}'");
            return string.Join(", ", quotedReasons);
        }
    }
}
