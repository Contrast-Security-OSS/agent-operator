using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record AgentConnectionResource(
        string TeamServerUri,
        SecretReference ApiKey,
        SecretReference ServiceKey,
        SecretReference UserName
    ) : INamespacedResource, IMutableResource;
}
