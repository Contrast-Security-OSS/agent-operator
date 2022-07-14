// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents
{
    public class JavaAgentPatcher : IAgentPatcher
    {
        public AgentInjectionType Type => AgentInjectionType.Java;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            yield return new V1EnvVar("JAVA_TOOL_OPTIONS", $"-javaagent:{context.ContrastMountPath}/contrast-agent.jar");
        }

        public string GetMountPath() => "/opt/contrast";
    }
}
