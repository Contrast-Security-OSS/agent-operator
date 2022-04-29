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

        public IEnumerable<ResourceIdentityPair<AgentInjectorResource>> GetMatchingInjectors(
            IEnumerable<ResourceIdentityPair<AgentInjectorResource>> readyInjectors,
            NamespacedResourceIdentity targetIdentity,
            IResourceWithPodTemplate targetResource)
        {
            foreach (var injector in readyInjectors)
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

        public IEnumerable<PodContainer> GetMatchingContainers(AgentInjectorResource injector,
                                                               IResourceWithPodTemplate targetResource)
        {
            var imagesPatterns = injector.Selector.ImagesPatterns;
            foreach (var container in targetResource.PodTemplate.Containers)
            {
                if (!imagesPatterns.Any()
                    || imagesPatterns.Any(p => _globMatcher.Matches(p, container.Image)))
                {
                    yield return container;
                }
            }
        }

        private bool MatchesLabel<T>(T targetResource, string key, string labelPattern) where T : IResourceWithPodTemplate
        {
            return targetResource.Labels.Any(
                label => string.Equals(key, label.Name, StringComparison.OrdinalIgnoreCase)
                         && _globMatcher.Matches(labelPattern, label.Value)
            );
        }
    }
}
