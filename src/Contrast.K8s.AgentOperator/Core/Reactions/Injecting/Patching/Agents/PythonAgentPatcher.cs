// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

public class PythonAgentPatcher : IAgentPatcher
{
    private readonly InjectorOptions _injectorOptions;
    public AgentInjectionType Type => AgentInjectionType.Python;

    public PythonAgentPatcher(InjectorOptions injectorOptions)
    {
        _injectorOptions = injectorOptions;
    }

    public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
    {
        yield return new V1EnvVar("PYTHONPATH", $"{context.AgentMountPath}:{context.AgentMountPath}/contrast/loader");
        if (_injectorOptions.EnablePythonRewriter)
        {
            yield return new V1EnvVar("CONTRAST__AGENT__PYTHON__REWRITE", "true");
        }
        yield return new V1EnvVar("__CONTRAST_USING_RUNNER", "true");
        yield return new V1EnvVar("CONTRAST__AGENT__LOGGER__PATH", $"{context.WritableMountPath}/logs/contrast_agent.log");
        yield return new V1EnvVar("CONTRAST_INSTALL_SOURCE", "kubernetes-operator");
    }
}
