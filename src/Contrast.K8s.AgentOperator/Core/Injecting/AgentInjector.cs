using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using JetBrains.Annotations;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    public interface IAgentInjector
    {
        ValueTask CalculateChanges(CancellationToken cancellationToken = default);
    }

    [UsedImplicitly]
    public class AgentInjector : IAgentInjector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IStateContainer _state;
        private readonly AgentInjectorMatcher _matcher;

        public AgentInjector(IStateContainer state, AgentInjectorMatcher matcher)
        {
            _state = state;
            _matcher = matcher;
        }

        public async ValueTask CalculateChanges(CancellationToken cancellationToken = default)
        {
            var readyAgentInjectors = await GetReadyAgentInjectors(cancellationToken);
            var context = new AgentInjectorContext(readyAgentInjectors);

            var rootResources = await _state.GetByType<IResourceWithPodSpec>(cancellationToken);
            foreach (var (identity, resource) in rootResources)
            {
                Logger.Trace($"Calculating changes needed for '{identity}'...");
                if (await _state.GetIsDirty(identity, cancellationToken))
                {
                    Logger.Trace($"Ignoring dirty '{identity}'.");
                }
                else
                {
                    CalculateChangesFor(context, identity, resource);
                }
            }
        }

        private void CalculateChangesFor(AgentInjectorContext context,
                                         NamespacedResourceIdentity targetIdentity,
                                         IResourceWithPodSpec targetResource)
        {
            var injectors = _matcher.GetMatchingInjectors(context, targetIdentity, targetResource).ToList();

            if (injectors.Count > 1)
            {
                Logger.Warn($"Multiple injectors select target '{targetIdentity}', "
                            + "the first alphabetically will be used to solve for ambiguity.");
            }

            var bestInjector = injectors.Count switch
            {
                1 => injectors.First(),
                > 1 => injectors.OrderBy(x => x.Identity.Name).First(),
                _ => null
            };

            if (bestInjector != null)
            {
                var containers = _matcher.GetMatchingContainers(bestInjector, targetResource).ToList();
                Logger.Trace($"Injector '{bestInjector.Identity}' selects containers '{string.Join(", ", containers.Select(x => x.Name))}'.");
            }
        }

        private async Task<IReadOnlyCollection<ResourceIdentityPair<AgentInjectorResource>>> GetReadyAgentInjectors(CancellationToken cancellationToken)
        {
            var readyAgentInjectors = new List<ResourceIdentityPair<AgentInjectorResource>>();
            var injectorIdentities = await _state.GetKeysByType<AgentInjectorResource>(cancellationToken);
            foreach (var identity in injectorIdentities)
            {
                if (await _state.GetReadyAgentInjectorById(identity.Name, identity.Namespace, cancellationToken) is { } agentInjector)
                {
                    readyAgentInjectors.Add(new ResourceIdentityPair<AgentInjectorResource>(identity, agentInjector));
                }
                else
                {
                    Logger.Info($"Ignoring the not ready '{identity}'.");
                }
            }

            return readyAgentInjectors;
        }
    }

    public record AgentInjectorContext(IReadOnlyCollection<ResourceIdentityPair<AgentInjectorResource>> ReadyInjectors);
}
