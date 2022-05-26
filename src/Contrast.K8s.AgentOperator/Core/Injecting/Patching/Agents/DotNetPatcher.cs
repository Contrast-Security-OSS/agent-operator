using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Injecting.Patching.Agents
{
    public class DotNetAgentPatcher : IAgentPatcher
    {
        public AgentInjectionType Type => AgentInjectionType.DotNetCore;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            yield return new V1EnvVar("CORECLR_PROFILER", "{8B2CE134-0948-48CA-A4B2-80DDAD9F5791}");
            yield return new V1EnvVar("CORECLR_PROFILER_PATH", $"{context.ContrastMountPath}/runtimes/linux-x64/native/ContrastProfiler.so");
            yield return new V1EnvVar("CORECLR_ENABLE_PROFILING", "1");
            yield return new V1EnvVar("CONTRAST_SOURCE", "kubernetes-operator");
            yield return new V1EnvVar("CONTRAST_CORECLR_INSTALL_DIRECTORY", context.ContrastMountPath);
        }

        public void PatchContainer(V1Container container, PatchingContext context)
        {
            // This assumes this patch occurs after our generic patches.
            // Either the users sets this on the pod manually, or we set it from our config file.
            // We also assume the default is true.
            var chainingEnabled = !string.Equals(
                GetFirstOrDefaultEnvVar(container.Env, "CONTRAST__AGENT__DOTNET__ENABLE_CHAINING")?.Value,
                "false",
                StringComparison.OrdinalIgnoreCase
            );

            // Only modify this if CONTRAST_EXISTING_LD_PRELOAD isn't already set. This is to prevent infinite loops.
            if (chainingEnabled
                && GetFirstOrDefaultEnvVar(container.Env, "LD_PRELOAD") is { Value: { } currentLdPreloadValue }
                && !string.IsNullOrWhiteSpace(currentLdPreloadValue)
                && GetFirstOrDefaultEnvVar(container.Env, "CONTRAST_EXISTING_LD_PRELOAD") is null)
            {
                container.Env.AddOrUpdate(new V1EnvVar("CONTRAST_EXISTING_LD_PRELOAD", currentLdPreloadValue));
                container.Env.AddOrUpdate(new V1EnvVar("LD_PRELOAD", $"{context.ContrastMountPath}/runtimes/linux-x64/native/ContrastChainLoader.so:{currentLdPreloadValue}"));
            }
        }

        private static V1EnvVar? GetFirstOrDefaultEnvVar(IEnumerable<V1EnvVar> collection, string name)
        {
            return collection.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
