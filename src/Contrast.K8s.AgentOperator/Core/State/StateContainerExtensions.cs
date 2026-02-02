// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults;
using Contrast.K8s.AgentOperator.Core.Reactions.Secrets;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State;

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
            context.AddFailureReason("Injector appears to have disappeared.");
        }
        else if (!injector.Enabled)
        {
            context.AddFailureReason("Injector is disabled and will be ignored.");
        }
        else
        {
            var connectionRef = injector.ConnectionReference ?? new AgentConnectionReference(@namespace, ClusterDefaults.AgentConnectionName(@namespace));
            var connectionResourceFound = await state.GetReadyAgentConnectionById(
                                              connectionRef.Name,
                                              connectionRef.Namespace,
                                              injector.ConnectionReference == null,
                                              context,
                                              cancellationToken
                                          ) != null;

            bool configurationIsValid;
            if (injector.ConfigurationReference is { } configurationRef)
            {
                var configurationResource = await state.GetById<AgentConfigurationResource>(configurationRef.Name, configurationRef.Namespace, cancellationToken);
                configurationIsValid = configurationResource != null;
                if (!configurationIsValid)
                {
                    context.AddFailureReason($"AgentConfiguration '{configurationRef.Namespace}/{configurationRef.Name}' could not be found.");
                }
            }
            else
            {
                // This field is optional (defaults to the namespace default, which can be missing).
                // However, if set, it must exist.
                configurationIsValid = true;
            }

            var pullSecretFound = injector.ImagePullSecret == null
                                  || await state.HasSecretKey(injector.ImagePullSecret, cancellationToken);
            if (!pullSecretFound
                && injector.ImagePullSecret is { } secret)
            {
                context.AddFailureReason($"Pull Secret '{secret.Namespace}/{secret.Name}' with key '{secret.Key}' could not be found.");
            }

            if (connectionResourceFound && configurationIsValid && pullSecretFound)
            {
                return new IsReadyResult<AgentInjectorResource>(injector);
            }
        }

        return new NotReadyResult<AgentInjectorResource>(context.Reasons);
    }

    private static async ValueTask<AgentConnectionResource?> GetReadyAgentConnectionById(this IStateContainer state,
                                                                                         string name,
                                                                                         string @namespace,
                                                                                         bool isNamespaceDefault,
                                                                                         ReadyContext readyContext,
                                                                                         CancellationToken cancellationToken = default)
    {
        var connection = await state.GetById<AgentConnectionResource>(name, @namespace, cancellationToken);
        if (connection != null)
        {
            //Check if token exists first, if it doesn't then validate the old auth values are there
            if (connection.Token != null)
            {
                var hasTokenSecret = await state.HasSecretKey(connection.Token, cancellationToken);
                if (!hasTokenSecret)
                {
                    readyContext.AddFailureReason(
                        $"Secret '{connection.Token.Namespace}/{connection.Token.Name}' with key '{connection.Token.Key}' could not be found.");
                }
                else
                {
                    return connection;
                }
            }
            else
            {
                var hasApiKeySecret = connection.ApiKey != null && await state.HasSecretKey(connection.ApiKey, cancellationToken);
                if (!hasApiKeySecret)
                {
                    readyContext.AddFailureReason(
                        connection.ApiKey == null
                            ? $"Required {nameof(connection.ApiKey)} secret missing in AgentConnection '{@namespace}/{name}'."
                            : $"Secret '{connection.ApiKey.Namespace}/{connection.ApiKey.Name}' with key '{connection.ApiKey.Key}' could not be found.");
                }

                var hasServiceKeySecret = connection.ServiceKey != null && await state.HasSecretKey(connection.ServiceKey, cancellationToken);
                if (!hasServiceKeySecret)
                {
                    readyContext.AddFailureReason(
                        connection.ServiceKey == null
                            ? $"Required {nameof(connection.ServiceKey)} secret missing in AgentConnection '{@namespace}/{name}'."
                            : $"Secret '{connection.ServiceKey.Namespace}/{connection.ServiceKey.Name}' with key '{connection.ServiceKey.Key}' could not be found.");
                }

                var hasUserNameSecret = connection.UserName != null && await state.HasSecretKey(connection.UserName, cancellationToken);
                if (!hasUserNameSecret)
                {
                    readyContext.AddFailureReason(
                        connection.UserName == null
                            ? $"Required {nameof(connection.UserName)} secret missing in AgentConnection '{@namespace}/{name}'."
                            : $"Secret '{connection.UserName.Namespace}/{connection.UserName.Name}' with key '{connection.UserName.Key}' could not be found.");
                }

                if (hasApiKeySecret
                    && hasServiceKeySecret
                    && hasUserNameSecret)
                {
                    return connection;
                }
            }
        }
        else
        {
            readyContext.AddFailureReason(isNamespaceDefault
                ? "AgentConnection was not specified, and no cluster default was found."
                : $"AgentConnection '{@namespace}/{name}' could not be found.");
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

        if (await state.GetById<AgentInjectorResource>(injectorName, injectorNamespace, cancellationToken) is { } injector)
        {
            var connectionRef = injector.ConnectionReference ?? new AgentConnectionReference(injectorNamespace, ClusterDefaults.AgentConnectionName(injectorNamespace));
            if(await state.GetById<AgentConnectionResource>(connectionRef.Name, connectionRef.Namespace, cancellationToken) is { } connection)
            {
                var secrets = new List<SecretResource>();

                var configurationRef = injector.ConfigurationReference ?? new AgentConfigurationReference(injectorNamespace, ClusterDefaults.AgentConfigurationName(injectorNamespace));
                var configuration = await state.GetById<AgentConfigurationResource>(configurationRef.Name, configurationRef.Namespace, cancellationToken);

                var imagePullSecret = injector.ImagePullSecret is { } imagePullSecretRef
                    ? await state.GetSecret(imagePullSecretRef, cancellationToken)
                    : null;

                if (imagePullSecret != null)
                {
                    secrets.Add(imagePullSecret);
                }

                var tokenSecret = connection.Token is { } tokenSecretRef
                    ? await state.GetSecret(tokenSecretRef, cancellationToken)
                    : null;

                if (tokenSecret != null)
                {
                    secrets.Add(tokenSecret);
                }

                var userNameSecret = connection.UserName is { } userNameSecretRef
                    ? await state.GetSecret(userNameSecretRef, cancellationToken)
                    : null;

                if (userNameSecret != null)
                {
                    secrets.Add(userNameSecret);
                }

                var serviceKeySecret = connection.UserName is { } serviceKeySecretRef
                    ? await state.GetSecret(serviceKeySecretRef, cancellationToken)
                    : null;

                if (serviceKeySecret != null)
                {
                    secrets.Add(serviceKeySecret);
                }

                var apiKeySecret = connection.UserName is { } apiKeySecretRef
                    ? await state.GetSecret(apiKeySecretRef, cancellationToken)
                    : null;

                if (apiKeySecret != null)
                {
                    secrets.Add(apiKeySecret);
                }

                var connectionVolumeSecret = connection.MountAsVolume == true
                                            ? new VolumeSecretReference(VolumeSecrets.GetConnectionVolumeSecretName(connectionRef.Name), VolumeSecrets.ConfigVolumeSecretKey, "/contrast/connection")
                                            : null;

                return new InjectorBundle(injector, connection, configuration, secrets, connectionVolumeSecret);
            }
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
                             IReadOnlyCollection<SecretResource> Secrets,
                             VolumeSecretReference? ConnectionVolumeSecret);

public abstract record ReadyResult<T>;

public record IsReadyResult<T>(T Resource) : ReadyResult<T>;

public record NotReadyResult<T>(IReadOnlyCollection<string> FailureReasons) : ReadyResult<T>
{
    public string FormatFailureReasons()
    {
        var quotedReasons = FailureReasons.Select(x => $"'{x}'");
        return string.Join(", ", quotedReasons);
    }
}
