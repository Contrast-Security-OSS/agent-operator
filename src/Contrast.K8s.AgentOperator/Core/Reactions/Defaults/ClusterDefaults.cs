// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public class ClusterDefaults
{
    private readonly IStateContainer _state;
    private readonly AgentInjectionTypeConverter _typeConverter;

    public ClusterDefaults(IStateContainer state, AgentInjectionTypeConverter typeConverter)
    {
        _state = state;
        _typeConverter = typeConverter;
    }

    public string GetDefaultAgentConfigurationName(string targetNamespace)
    {
        return "default-agent-configuration-" + GetShortHash(targetNamespace);
    }

    public string GetDefaultAgentConnectionName(string targetNamespace)
    {
        return "default-agent-connection-" + GetShortHash(targetNamespace);
    }

    public string GetDefaultAgentConnectionSecretName(string targetNamespace)
    {
        return "default-agent-connection-secret-" + GetShortHash(targetNamespace);
    }

    public string GetDefaultPullSecretName(string targetNamespace, AgentInjectionType agentType)
    {
        var type = _typeConverter.GetStringFromType(agentType);
        return $"default-agent-injector-pullsecret-{type}-{GetShortHash(targetNamespace)}";
    }

    public string GetDefaultAgentInjectorName(string targetNamespace, AgentInjectionType agentType)
    {
        var type = _typeConverter.GetStringFromType(agentType);
        return $"default-agent-injector-{type}-{GetShortHash(targetNamespace)}";
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

    private static string GetShortHash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return HexConverter.ToLowerHex(hash, 8);
    }


}
