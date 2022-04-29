using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using JetBrains.Annotations;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    [UsedImplicitly]
    public class MatchInjectorsHandler : INotificationHandler<StateModified>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IStateContainer _state;
        private readonly AgentInjectorMatcher _matcher;
        private readonly IMediator _mediator;

        public MatchInjectorsHandler(IStateContainer state, AgentInjectorMatcher matcher, IMediator mediator)
        {
            _state = state;
            _matcher = matcher;
            _mediator = mediator;
        }

        public async Task Handle(StateModified notification, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Trace("Cluster state changed, re-calculating injection points.");

            await Handle(cancellationToken);

            Logger.Trace($"Completed re-calculating injection points after {stopwatch.ElapsedMilliseconds}ms.");
        }

        private async ValueTask Handle(CancellationToken cancellationToken = default)
        {
            var readyAgentInjectors = await GetReadyAgentInjectors(cancellationToken);

            var rootResources = await _state.GetByType<IResourceWithPodTemplate>(cancellationToken);
            foreach (var target in rootResources)
            {
                Logger.Trace($"Calculating changes needed for '{target.Identity}'...");
                var bestInjector = GetBestInjector(readyAgentInjectors, target);
                await _mediator.Publish(new InjectorMatched(target, bestInjector), cancellationToken);
            }
        }

        private ResourceIdentityPair<AgentInjectorResource>? GetBestInjector(IEnumerable<ResourceIdentityPair<AgentInjectorResource>> readyAgentInjectors,
                                                                             ResourceIdentityPair<IResourceWithPodTemplate> target)
        {
            var injectors = _matcher.GetMatchingInjectors(readyAgentInjectors, target.Identity, target.Resource).ToList();

            if (injectors.Count > 1)
            {
                Logger.Warn($"Multiple injectors select target '{target.Identity}', "
                            + "the first alphabetically will be used to solve for ambiguity.");
            }

            var bestInjector = injectors.Count switch
            {
                1 => injectors.First(),
                > 1 => injectors.OrderBy(x => x.Identity.Name).First(),
                _ => null
            };

            return bestInjector;
        }

        private async ValueTask<IReadOnlyCollection<ResourceIdentityPair<AgentInjectorResource>>> GetReadyAgentInjectors(CancellationToken cancellationToken)
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
}
