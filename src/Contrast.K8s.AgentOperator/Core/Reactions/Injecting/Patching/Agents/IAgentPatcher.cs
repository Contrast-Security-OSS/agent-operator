// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents
{
    public interface IAgentPatcher
    {
        AgentInjectionType Type { get; }

        IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context);

        public void PatchContainer(V1Container container, PatchingContext context)
        {
        }

        public string? GetMountPath() => null;
    }
}
