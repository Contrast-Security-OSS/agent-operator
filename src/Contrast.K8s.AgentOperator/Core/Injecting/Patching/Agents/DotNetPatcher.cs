using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Injecting.Patching.Agents
{
    public class DotNetAgentPatcher : IAgentPatcher
    {
        public AgentInjectionType Type => AgentInjectionType.DotNetCore;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            // TODO Double check this for correctness.
            yield return new V1EnvVar("CORECLR_PROFILER", "{8B2CE134-0948-48CA-A4B2-80DDAD9F5791}");
            yield return new V1EnvVar("CORECLR_PROFILER_PATH", $"{context.ContrastMountPath}/runtimes/linux-x64/native/ContrastProfiler.dll");
            yield return new V1EnvVar("CORECLR_ENABLE_PROFILING", "1");
            yield return new V1EnvVar("CONTRAST_INSTALL_SOURCE", "kubernetes-operator");
        }
    }
}
