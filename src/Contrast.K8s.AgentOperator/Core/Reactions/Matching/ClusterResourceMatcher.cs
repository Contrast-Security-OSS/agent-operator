// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Matching;

public class ClusterResourceMatcher
{
    private readonly IGlobMatcher _matcher;

    public ClusterResourceMatcher(IGlobMatcher matcher)
    {
        _matcher = matcher;
    }

    public IReadOnlyCollection<ResourceIdentityPair<T>> GetMatchingBases<T>(
        IEnumerable<ResourceIdentityPair<T>> clusterResources,
        string namespaceName,
        NamespaceResource namespaceResource) where T : IClusterResource
    {
        //If NamespacePatterns and NamespaceLabelPatterns are empty return ClusterResource
        //Otherwise match on NamespacePatterns OR NamespaceLabelPatterns
        return clusterResources.Where(x =>
                (x.Resource.NamespacePatterns.Count == 0 && x.Resource.NamespaceLabelPatterns.Count == 0)
                || MatchesNamespace(x.Resource, namespaceName, namespaceResource))
            .ToList();
    }

    public IReadOnlyCollection<ResourceIdentityPair<ClusterAgentInjectorResource>> GetMatchingBasesForAgent(
        IEnumerable<ResourceIdentityPair<ClusterAgentInjectorResource>> clusterResources,
        string namespaceName,
        NamespaceResource namespaceResource,
        AgentInjectionType agentType)
    {
        //Match on NamespacePatterns OR NamespaceLabelPatterns then on AgentType
        return clusterResources.Where(x =>
                MatchesNamespace(x.Resource, namespaceName, namespaceResource)
                && x.Resource.Template.Type == agentType)
            .ToList();
    }

    private bool MatchesNamespace(IClusterResource clusterResource, string namespaceName,
        NamespaceResource namespaceResource)
    {
        var matchesName = clusterResource.NamespacePatterns.Any(pattern => _matcher.Matches(pattern, namespaceName));
        var matchesLabel = clusterResource.NamespaceLabelPatterns.Count > 0 && clusterResource.NamespaceLabelPatterns.All(x => MatchesLabel(namespaceResource, x.Key, x.Value));
        return matchesName || matchesLabel;
    }


    private bool MatchesLabel(NamespaceResource resource, string key, string labelPattern)
    {
        return resource.Labels.Any(label => string.Equals(key, label.Name, StringComparison.OrdinalIgnoreCase)
                                                  && _matcher.Matches(labelPattern, label.Value));
    }
}
