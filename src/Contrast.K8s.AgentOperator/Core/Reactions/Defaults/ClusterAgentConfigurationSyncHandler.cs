using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s.Autorest;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults
{
    public class ClusterAgentConfigurationSyncHandler : INotificationHandler<StateModified>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IStateContainer _state;
        private readonly IGlobMatcher _matcher;
        private readonly OperatorOptions _operatorOptions;
        private readonly IResourceComparer _comparer;
        private readonly IKubernetesClient _kubernetesClient;
        private readonly ClusterDefaults _clusterDefaults;
        private readonly IReactionHelper _reactionHelper;

        public ClusterAgentConfigurationSyncHandler(IStateContainer state,
                                                    IGlobMatcher matcher,
                                                    OperatorOptions operatorOptions,
                                                    IResourceComparer comparer,
                                                    IKubernetesClient kubernetesClient,
                                                    ClusterDefaults clusterDefaults,
                                                    IReactionHelper reactionHelper)
        {
            _state = state;
            _matcher = matcher;
            _operatorOptions = operatorOptions;
            _comparer = comparer;
            _kubernetesClient = kubernetesClient;
            _clusterDefaults = clusterDefaults;
            _reactionHelper = reactionHelper;
        }

        public async Task Handle(StateModified notification, CancellationToken cancellationToken)
        {
            if (!await _reactionHelper.CanReact(cancellationToken))
            {
                return;
            }

            var allNamespaces = await _clusterDefaults.GetAllNamespaces(cancellationToken);
            var validNamespaces = await _clusterDefaults.GetValidNamespacesForDefaults(cancellationToken);
            var availableConfigurations = await GetAvailableClusterResources(cancellationToken);

            foreach (var @namespace in allNamespaces)
            {
                var targetConfigurationName = _clusterDefaults.GetDefaultAgentConfigurationName(@namespace);
                if (await _state.GetIsDirty<AgentConfigurationResource>(targetConfigurationName, @namespace, cancellationToken))
                {
                    continue;
                }

                var existingResource = await _state.GetById<AgentConfigurationResource>(targetConfigurationName, @namespace, cancellationToken);
                var isValidNamespace = validNamespaces.Any(x => string.Equals(x, @namespace, StringComparison.OrdinalIgnoreCase));

                if (isValidNamespace
                    && GetBestConfigurationForNamespace(availableConfigurations, @namespace) is { } bestConfiguration)
                {
                    if (!_comparer.AreEqual(existingResource, bestConfiguration.Resource.Template))
                    {
                        // Should update.
                        await CreateOrUpdate(bestConfiguration, targetConfigurationName, @namespace, cancellationToken);
                    }
                }
                else
                {
                    if (existingResource != null)
                    {
                        // Should delete.
                        await Delete(@namespace, targetConfigurationName, cancellationToken);
                    }
                }
            }
        }

        private async Task CreateOrUpdate(ResourceIdentityPair<ClusterAgentConfigurationResource> bestConfiguration,
                                          string targetConfigurationName,
                                          string @namespace,
                                          CancellationToken cancellationToken)
        {
            Logger.Info($"Out-dated AgentConfiguration '{@namespace}/{targetConfigurationName}' entity detected, preparing to create/patch.");

            await _state.MarkAsDirty<AgentConfigurationResource>(targetConfigurationName, @namespace, cancellationToken);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var annotations = _clusterDefaults.GetAnnotationsForManagedResources(bestConfiguration.Identity.Name, bestConfiguration.Identity.Namespace);

                var builder = new StringBuilder();
                foreach (var yamlKey in bestConfiguration.Resource.Template.YamlKeys)
                {
                    // Hard code the new line to Linux.
                    builder.Append(yamlKey.Key).Append(": ").Append(yamlKey.Value).Append("\n");
                }

                var yaml = builder.ToString();

                await _kubernetesClient.Save(new V1Beta1AgentConfiguration
                {
                    Metadata = new V1ObjectMeta(name: targetConfigurationName, namespaceProperty: @namespace, annotations: annotations),
                    Spec = new V1Beta1AgentConfiguration.AgentConfigurationSpec
                    {
                        Yaml = yaml
                    }
                });
                Logger.Info($"Updated AgentConfiguration '{@namespace}/{targetConfigurationName}' after {stopwatch.ElapsedMilliseconds}ms.");
            }
            catch (HttpOperationException e)
            {
                Logger.Warn(e, $"An error occurred. Response body: {e.Response.Content}");
            }
            catch (Exception e)
            {
                Logger.Debug(e);
            }
        }

        private async Task Delete(string @namespace,
                                  string targetConfigurationName,
                                  CancellationToken cancellationToken)
        {
            Logger.Info($"Superfluous AgentConfiguration '{@namespace}/{targetConfigurationName}' entity detected, preparing to delete.");
            await _state.MarkAsDirty<AgentConfigurationResource>(targetConfigurationName, @namespace, cancellationToken);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _kubernetesClient.Delete<V1Beta1AgentConfiguration>(targetConfigurationName, @namespace);
                Logger.Info($"Deleted AgentConfiguration '{@namespace}/{targetConfigurationName}' after {stopwatch.ElapsedMilliseconds}ms.");
            }
            catch (HttpOperationException e)
            {
                Logger.Warn(e, $"An error occurred. Response body: {e.Response.Content}");
            }
            catch (Exception e)
            {
                Logger.Debug(e);
            }
        }

        private ResourceIdentityPair<ClusterAgentConfigurationResource>? GetBestConfigurationForNamespace(
            IEnumerable<ResourceIdentityPair<ClusterAgentConfigurationResource>> availableConfigurations, string @namespace)
        {
            var matchingDefaultConfigurations =
                availableConfigurations.Where(x => x.Resource.NamespacePatterns.Any(pattern => _matcher.Matches(pattern, @namespace))).ToList();
            if (matchingDefaultConfigurations.Count > 1)
            {
                Logger.Warn("Multiple ClusterAgentConfiguration entities "
                            + $"[{string.Join(", ", matchingDefaultConfigurations.Select(x => x.Identity.Name))}] match the namespace '{@namespace}'. "
                            + "Selecting first alphabetically to solve for ambiguity.");
                return matchingDefaultConfigurations.OrderBy(x => x.Identity.Name).First();
            }

            return matchingDefaultConfigurations.SingleOrDefault();
        }

        private async Task<IReadOnlyCollection<ResourceIdentityPair<ClusterAgentConfigurationResource>>> GetAvailableClusterResources(
            CancellationToken cancellationToken)
        {
            var resources = new List<ResourceIdentityPair<ClusterAgentConfigurationResource>>();
            foreach (var configuration in await _state.GetByType<ClusterAgentConfigurationResource>(cancellationToken))
            {
                if (string.Equals(configuration.Identity.Namespace, _operatorOptions.Namespace, StringComparison.OrdinalIgnoreCase))
                {
                    resources.Add(configuration);
                }
            }

            return resources;
        }
    }
}
