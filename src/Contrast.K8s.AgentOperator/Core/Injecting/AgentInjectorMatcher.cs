using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    public class AgentInjectorMatcher
    {
        private readonly IGlobMatcher _globMatcher;

        public AgentInjectorMatcher(IGlobMatcher globMatcher)
        {
            _globMatcher = globMatcher;
        }

        public IEnumerable<ResourceIdentityPair<AgentInjectorResource>> GetMatchingInjectors(AgentInjectorContext context,
                                                                                             NamespacedResourceIdentity targetIdentity,
                                                                                             IResourceWithPodSpec targetResource)
        {
            var agentInjectorResources = context.ReadyInjectors;
            foreach (var injector in agentInjectorResources)
            {
                var (_, labelPatterns, namespaces) = injector.Resource.Selector;

                var matchesNamespace = namespaces.Contains(targetIdentity.Namespace, StringComparer.OrdinalIgnoreCase);
                var matchesLabel = !labelPatterns.Any() || labelPatterns.Any(x => MatchesLabel(targetResource, x.Key, x.Value));
                if (matchesNamespace && matchesLabel)
                {
                    yield return injector;
                }
            }
        }

        public IEnumerable<PodContainer> GetMatchingContainers(ResourceIdentityPair<AgentInjectorResource> injector,
                                                               IResourceWithPodSpec targetResource)
        {
            var imagesPatterns = injector.Resource.Selector.ImagesPatterns;
            foreach (var container in targetResource.Containers)
            {
                if (!imagesPatterns.Any()
                    || imagesPatterns.Any(p => _globMatcher.Matches(p, container.Image)))
                {
                    yield return container;
                }
            }
        }

        private bool MatchesLabel<T>(T targetResource, string key, string labelPattern) where T : IResourceWithPodSpec
        {
            return targetResource.Labels.Any(
                label => string.Equals(key, label.Name, StringComparison.OrdinalIgnoreCase)
                         && _globMatcher.Matches(labelPattern, label.Value)
            );
        }
    }
}
