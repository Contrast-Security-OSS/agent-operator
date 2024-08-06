// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

public class PhpAgentPatcher : IAgentPatcher
{
    public AgentInjectionType Type => AgentInjectionType.Php;

    public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
    {
        yield return new V1EnvVar("PHP_INI_SCAN_DIR", $":{context.AgentMountPath}/ini/");
        yield return new V1EnvVar("CONTRAST__AGENT__LOGGER__PATH", $"{context.WritableMountPath}/logs/contrast_agent.log");
        yield return new V1EnvVar("CONTRAST_INSTALL_SOURCE", "kubernetes-operator");
    }

    public string GetOverrideAgentMountPath() => "/usr/local/lib/contrast/php";
}
