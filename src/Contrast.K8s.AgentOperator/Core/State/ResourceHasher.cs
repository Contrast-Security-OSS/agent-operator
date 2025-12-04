// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.State;

public interface IResourceHasher
{
    string GetHash(AgentInjectorResource injector,
                   AgentConnectionResource connection,
                   AgentConfigurationResource? configuration,
                   IEnumerable<SecretResource> secretResources);
}

public class ResourceHasher : IResourceHasher
{
    private readonly KubernetesJsonSerializer _jsonSerializer;

    public ResourceHasher(KubernetesJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
    }

    public string GetHash(AgentInjectorResource injector,
                          AgentConnectionResource connection,
                          AgentConfigurationResource? configuration,
                          IEnumerable<SecretResource> secretResources)
    {
        return GetHashImpl(injector, connection, configuration, secretResources);
    }

    private string GetHashImpl(params object?[] objects)
    {
        var builder = new StringBuilder();
        foreach (var o in objects)
        {
            builder.Append(GetHashImpl(o));
        }

        return HashHelper.Sha256(builder.ToString());
    }

    private string GetHashImpl(object? o)
    {
        var json = _jsonSerializer.SerializeObject(o ?? "<null>");
        return HashHelper.Sha256(json);
    }


}
