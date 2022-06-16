// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        AgentConfigurationReference ConfigurationReference,
        SecretReference? ImagePullSecret,
        string ImagePullPolicy
    ) : INamespacedResource;
}
