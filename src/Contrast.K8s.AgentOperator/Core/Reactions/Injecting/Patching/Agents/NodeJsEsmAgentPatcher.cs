// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

public class NodeJsEsmAgentPatcher : IAgentPatcher
{
    public AgentInjectionType Type => AgentInjectionType.NodeJsEsm;

    public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
    {
        // https://nodejs.org/api/cli.html#node_optionsoptions
        yield return new V1EnvVar("NODE_OPTIONS", $"--import {context.AgentMountPath}/node_modules/@contrast/agent/lib/esm-loader.mjs");
        yield return new V1EnvVar("CONTRAST__AGENT__LOGGER__PATH", $"{context.WritableMountPath}/logs/contrast_agent.log");
        yield return new V1EnvVar("CONTRAST__AGENT__SECURITY_LOGGER__PATH", $"{context.WritableMountPath}/logs/contrast_agent_cef.log");
    }
}
