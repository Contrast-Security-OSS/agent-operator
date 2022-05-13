using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Injecting.Patching.Agents
{
    public class PhpAgentPatcher : IAgentPatcher
    {
        public AgentInjectionType Type => AgentInjectionType.Php;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            // TODO Double check this for correctness.
            yield return new V1EnvVar("CONTRAST_MOUNT_PATH", context.ContrastMountPath);
            yield return new V1EnvVar("PHP_INI_SCAN_DIR", $":{context.ContrastMountPath}/ini/php.ini");
        }
    }
}
