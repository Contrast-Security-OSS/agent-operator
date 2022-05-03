using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Injecting.Patching.Agents
{
    public interface IAgentPatcher
    {
        AgentInjectionType Type { get; }

        IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context);
    }
}
