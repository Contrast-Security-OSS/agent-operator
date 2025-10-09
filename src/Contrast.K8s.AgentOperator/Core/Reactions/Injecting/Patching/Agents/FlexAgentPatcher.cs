﻿// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

public class FlexAgentPatcher : IAgentPatcher
{
    public AgentInjectionType Type => AgentInjectionType.Flex;

    public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
    {
        yield return new V1EnvVar("LD_PRELOAD", GetInjectorPreloadPath(context));
        yield return new V1EnvVar("CONTRAST_INSTALLATION_TOOL", "KUBERNETES_OPERATOR");

        yield return new V1EnvVar("CONTRAST_FLEX_AGENTS_PARENT_DIR", context.AgentMountPath);
        yield return new V1EnvVar("CONTRAST_FLEX_COMMS_PARENT_DIR", context.AgentMountPath);
        yield return new V1EnvVar("CONTRAST_FLEX_WRITABLE_DIR", context.WritableMountPath);
    }

    public void PatchContainer(V1Container container, PatchingContext context)
    {
        // Only modify this if CONTRAST_EXISTING_LD_PRELOAD isn't already set. This is to prevent infinite loops.
        if (container.Env.FirstOrDefault("LD_PRELOAD") is { Value: { } currentLdPreloadValue }
            && !string.IsNullOrWhiteSpace(currentLdPreloadValue)
            && !currentLdPreloadValue.Contains("agent_injector.so", StringComparison.OrdinalIgnoreCase)
            && container.Env.FirstOrDefault("CONTRAST_EXISTING_LD_PRELOAD") is null)
        {
            container.Env.AddOrUpdate(new V1EnvVar("CONTRAST_EXISTING_LD_PRELOAD", currentLdPreloadValue));
            container.Env.AddOrUpdate(new V1EnvVar("LD_PRELOAD",
                $"{GetInjectorPreloadPath(context)}:{currentLdPreloadValue}"));
        }
    }

    private static string GetInjectorPreloadPath(PatchingContext context) => $"{context.AgentMountPath}/injector/agent_injector.so";

}
