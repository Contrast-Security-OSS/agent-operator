using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Injecting;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Monitoring
{
    public class CalculateInjectorStatusHandler : INotificationHandler<StateModified>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IStateContainer _state;
        private readonly AgentInjectorMatcher _matcher;

        public CalculateInjectorStatusHandler(IStateContainer state, AgentInjectorMatcher matcher)
        {
            _state = state;
            _matcher = matcher;
        }

        public async Task Handle(StateModified notification, CancellationToken cancellationToken)
        {
            if (!await _state.GetHasSettled(cancellationToken))
            {
                return;
            }

            var injectors = await _state.GetByType<AgentInjectorResource>(cancellationToken);
            var podTemplates = await _state.GetByType<IResourceWithPodTemplate>(cancellationToken);
            var pods = await _state.GetByType<PodResource>(cancellationToken);

            var totalPodsCount = 0;
            var pendingPodsCount = 0;

            var totalInjectorsCount = 0;
            var pendingInjectorsCount = 0;

            foreach (var injector in injectors)
            {
                var injectorTotalPodsCount = 0;
                var injectorPendingPodsCount = 0;
                foreach (var podTemplate in podTemplates)
                {
                    if (_matcher.InjectorMatchesTarget(injector, podTemplate))
                    {
                        var matchingPods = GetMatchingPods(pods, podTemplate.Resource.Selector).ToList();

                        injectorTotalPodsCount += matchingPods.Count;
                        injectorPendingPodsCount += matchingPods.Count(x => !x.Resource.IsInjected);
                    }
                }

                totalPodsCount += injectorTotalPodsCount;
                pendingPodsCount += injectorPendingPodsCount;

                totalInjectorsCount++;
                if (injectorPendingPodsCount > 0)
                {
                    pendingInjectorsCount++;
                }
            }

            if (pendingInjectorsCount > 0)
            {
                Logger.Info($"{pendingInjectorsCount}/{totalInjectorsCount} injectors are waiting for {pendingPodsCount}/{totalPodsCount} pods to converge.");
            }
        }

        private IEnumerable<ResourceIdentityPair<PodResource>> GetMatchingPods(IEnumerable<ResourceIdentityPair<PodResource>> pods,
                                                                               PodSelector podSelector)
        {
            return pods.Where(pod => PodMatchesSelector(pod.Resource, podSelector));
        }

        private bool PodMatchesSelector(PodResource podResource, PodSelector selector)
        {
            foreach (var expression in selector.Expressions)
            {
                var value = podResource.Labels.FirstOrDefault(x => string.Equals(x.Name, expression.Key, StringComparison.OrdinalIgnoreCase))?.Value;
                var matches = expression.Operator switch
                {
                    LabelMatchOperation.In => expression.Values.Contains(value, StringComparer.OrdinalIgnoreCase),
                    LabelMatchOperation.NotIn => !expression.Values.Contains(value, StringComparer.OrdinalIgnoreCase),
                    LabelMatchOperation.Exists => value != null,
                    LabelMatchOperation.DoesNotExist => value == null,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (!matches)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
