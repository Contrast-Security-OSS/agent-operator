// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

public record ContainerImageReference(string Registry, string Name, string Tag)
{
    public string GetFullyQualifiedContainerImageName() => $"{Registry}/{Name}:{Tag}".ToLowerInvariant();
}
