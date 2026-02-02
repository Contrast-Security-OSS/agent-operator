// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public class ClusterDefaultsHelper
{
    private readonly IStateContainer _state;

    public ClusterDefaultsHelper(IStateContainer state)
    {
        _state = state;
    }

    public async ValueTask<IReadOnlyCollection<string>> GetAllNamespaces(CancellationToken cancellationToken = default)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        var keys = await _state.GetKeysByType<NamespaceResource>(cancellationToken);
        foreach (var id in keys)
        {
            var @namespace = id.Name.ToLowerInvariant();
            namespaces.Add(@namespace);
        }

        return namespaces;
    }

    public async ValueTask<IReadOnlyCollection<string>> GetValidNamespacesForDefaults(CancellationToken cancellationToken = default)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        var keys = await _state.GetKeysByType<AgentInjectorResource>(cancellationToken);
        foreach (var id in keys)
        {
            var @namespace = id.Namespace.ToLowerInvariant();
            namespaces.Add(@namespace);
        }

        return namespaces;
    }

    public IReadOnlyCollection<string> GetSystemNamespaces()
    {
        return new HashSet<string> { "kube-system", "kube-node-lease", "kube-public", "gatekeeper-system" };
    }
}
