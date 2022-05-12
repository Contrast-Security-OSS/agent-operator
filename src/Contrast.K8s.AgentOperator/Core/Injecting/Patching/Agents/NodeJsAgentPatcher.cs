using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Injecting.Patching.Agents
{
    public class NodeJsAgentPatcher : IAgentPatcher
    {
        public AgentInjectionType Type => AgentInjectionType.NodeJs;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            // TODO Double check this for correctness.

            // https://nodejs.org/api/cli.html#node_optionsoptions
            yield return new V1EnvVar("NODE_OPTIONS", "--require @contrast/agent");

            // https://nodejs.org/api/cli.html#node_pathpath
            yield return new V1EnvVar("NODE_PATH", $"{context.ContrastMountPath}");
        }
    }
}
