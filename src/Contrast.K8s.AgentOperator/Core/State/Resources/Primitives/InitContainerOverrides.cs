// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record InitContainerOverrides(
        V1SecurityContext? SecurityContext,
        V1ResourceRequirements? Resources
    );
}
