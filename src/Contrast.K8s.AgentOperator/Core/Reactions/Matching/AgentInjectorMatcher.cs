// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Matching;

public class AgentInjectorMatcher
{
    private readonly IGlobMatcher _globMatcher;

    public AgentInjectorMatcher(IGlobMatcher globMatcher)
    {
        _globMatcher = globMatcher;
    }

    public IEnumerable<ResourceIdentityPair<AgentInjectorResource>> GetMatchingInjectors(
        IEnumerable<ResourceIdentityPair<AgentInjectorResource>> readyInjectors,
        ResourceIdentityPair<IResourceWithPodTemplate> target)
    {
        return readyInjectors.Where(injector => InjectorMatchesTarget(injector, target));
    }

    private bool InjectorMatchesTarget(ResourceIdentityPair<AgentInjectorResource> injector,
                                       ResourceIdentityPair<IResourceWithPodTemplate> target)
    {
        var (_, labelPatterns, namespaces) = injector.Resource.Selector;

        var matchesNamespace = namespaces.Contains(target.Identity.Namespace, StringComparer.OrdinalIgnoreCase);
        var matchesLabel = !labelPatterns.Any() || labelPatterns.All(x => MatchesLabel(target.Resource, x.Key, x.Value));
        return matchesNamespace && matchesLabel;
    }

    private bool MatchesLabel<T>(T targetResource, string key, string labelPattern) where T : IResourceWithPodTemplate
    {
        return targetResource.Labels.Any(
            label => string.Equals(key, label.Name, StringComparison.OrdinalIgnoreCase)
                     && _globMatcher.Matches(labelPattern, label.Value)
        );
    }
}
