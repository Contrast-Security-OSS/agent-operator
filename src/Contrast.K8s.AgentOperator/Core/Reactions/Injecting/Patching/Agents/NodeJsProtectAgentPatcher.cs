// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents
{
    public class NodeJsProtectAgentPatcher : IAgentPatcher
    {
        public AgentInjectionType Type => AgentInjectionType.NodeJsProtect;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            // https://nodejs.org/api/cli.html#node_optionsoptions
            yield return new V1EnvVar("NODE_OPTIONS", $"--require {context.AgentMountPath}/node_modules/@contrast/protect-agent");
            yield return new V1EnvVar("CONTRAST__AGENT__LOGGER__PATH", $"{context.WritableMountPath}/logs/contrast_agent.log");
        }
    }
}
