using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record SecretResource(IReadOnlyCollection<string> Keys, string DataHash) : INamespacedResource, IMutableResource;
}
