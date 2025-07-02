// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

public class FlexAgentPatcher : IAgentPatcher
{
    public AgentInjectionType Type => AgentInjectionType.Flex;

    public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
    {
        yield return new V1EnvVar("LD_PRELOAD", $"{context.AgentMountPath}/injector/agent_injector.so");
        yield return new V1EnvVar("CONTRAST_INSTALLATION_TOOL", "KUBERNETES_OPERATOR");

        yield return new V1EnvVar("CONTRAST_FLEX_AGENTS_DIR", $"{context.AgentMountPath}/agents");
        yield return new V1EnvVar("CONTRAST_FLEX_COMMS_DIR", $"{context.AgentMountPath}/comms");
        yield return new V1EnvVar("CONTRAST_FLEX_WRITABLE_DIR", context.WritableMountPath);
    }

    public void PatchContainer(V1Container container, PatchingContext context)
    {
        // Only modify this if CONTRAST_EXISTING_LD_PRELOAD isn't already set. This is to prevent infinite loops.
        if (GetFirstOrDefaultEnvVar(container.Env, "LD_PRELOAD") is { Value: { } currentLdPreloadValue }
            && !string.IsNullOrWhiteSpace(currentLdPreloadValue)
            && !currentLdPreloadValue.Contains("agent_injector.so", StringComparison.OrdinalIgnoreCase)
            && GetFirstOrDefaultEnvVar(container.Env, "CONTRAST_EXISTING_LD_PRELOAD") is null)
        {
            container.Env.AddOrUpdate(new V1EnvVar("CONTRAST_EXISTING_LD_PRELOAD", currentLdPreloadValue));
            container.Env.AddOrUpdate(new V1EnvVar("LD_PRELOAD",
                $"{context.AgentMountPath}/injector/agent_injector.so:{currentLdPreloadValue}"));
        }
    }

    private static V1EnvVar? GetFirstOrDefaultEnvVar(IEnumerable<V1EnvVar> collection, string name)
    {
        return collection.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
