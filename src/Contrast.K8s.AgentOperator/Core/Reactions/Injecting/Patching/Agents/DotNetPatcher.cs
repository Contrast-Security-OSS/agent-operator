// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents
{
    public class DotNetAgentPatcher : IAgentPatcher
    {
        private readonly InjectorOptions _injectorOptions;
        public AgentInjectionType Type => AgentInjectionType.DotNetCore;

        public DotNetAgentPatcher(InjectorOptions injectorOptions)
        {
            _injectorOptions = injectorOptions;
        }

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            if (_injectorOptions.EnableEarlyChaining)
            {
                yield return new V1EnvVar("LD_PRELOAD",
                    $"{context.AgentMountPath}/runtimes/linux-x64/native/ContrastChainLoader.so");
            }
            else
            {
                yield return new V1EnvVar("CORECLR_PROFILER", "{8B2CE134-0948-48CA-A4B2-80DDAD9F5791}");
                yield return new V1EnvVar("CORECLR_PROFILER_PATH", $"{context.AgentMountPath}/runtimes/linux-x64/native/ContrastProfiler.so");
                yield return new V1EnvVar("CORECLR_ENABLE_PROFILING", "1");
            }

            yield return new V1EnvVar("CONTRAST_SOURCE", "kubernetes-operator");
            yield return new V1EnvVar("CONTRAST_CORECLR_INSTALL_DIRECTORY", context.AgentMountPath);
            yield return new V1EnvVar("CONTRAST_CORECLR_DATA_DIRECTORY", context.WritableMountPath);
            yield return new V1EnvVar("CONTRAST_CORECLR_LOGS_DIRECTORY", $"{context.WritableMountPath}/logs");
            yield return new V1EnvVar("CONTRAST__AGENT__DOTNET__ENABLE_FILE_WATCHING", "false");
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

            // Only modify this if CONTRAST_EXISTING_LD_PRELOAD isn't already set, or we are not already set from early chaining. This is to prevent infinite loops.
            if (chainingEnabled
                && GetFirstOrDefaultEnvVar(container.Env, "LD_PRELOAD") is { Value: { } currentLdPreloadValue }
                && !string.IsNullOrWhiteSpace(currentLdPreloadValue)
                && !currentLdPreloadValue.Contains("ContrastChainLoader.so", StringComparison.OrdinalIgnoreCase)
                && GetFirstOrDefaultEnvVar(container.Env, "CONTRAST_EXISTING_LD_PRELOAD") is null)
            {
                container.Env.AddOrUpdate(new V1EnvVar("CONTRAST_EXISTING_LD_PRELOAD", currentLdPreloadValue));
                container.Env.AddOrUpdate(new V1EnvVar("LD_PRELOAD",
                    $"{context.AgentMountPath}/runtimes/linux-x64/native/ContrastChainLoader.so:{currentLdPreloadValue}"));
            }
        }

        private static V1EnvVar? GetFirstOrDefaultEnvVar(IEnumerable<V1EnvVar> collection, string name)
        {
            return collection.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
