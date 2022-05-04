using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State.Resources
{
    public record AgentInjectorResource(
        bool Enabled,
        AgentInjectionType Type,
        ContainerImageReference Image,
        ResourceWithPodSpecSelector Selector,
        AgentInjectorConnectionReference ConnectionReference,
        AgentConfigurationReference? ConfigurationReference,
        SecretReference? ImagePullSecretReference
    ) : INamespacedResource;
}
