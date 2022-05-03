using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Injecting.Patching.Agents
{
    public class JavaAgentPatcher : IAgentPatcher
    {
        public AgentInjectionType Type => AgentInjectionType.Java;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            // TODO Double check this for correctness.
            yield return new V1EnvVar("JAVA_TOOL_OPTIONS", $"-javaagent:{context.ContrastMountPath}/contrast.jar");
        }
    }
}
