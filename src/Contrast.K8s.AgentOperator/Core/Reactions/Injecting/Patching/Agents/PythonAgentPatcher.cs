// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using System;
using System.Collections.Generic;

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
        yield return new V1EnvVar("PYTHONPATH", GetAgentPythonPath(context));
        if (_injectorOptions.EnablePythonRewriter)
        {
            yield return new V1EnvVar("CONTRAST__AGENT__PYTHON__REWRITE", "true");
        }
        yield return new V1EnvVar("__CONTRAST_USING_RUNNER", "true");
        yield return new V1EnvVar("CONTRAST__AGENT__LOGGER__PATH", $"{context.WritableMountPath}/logs/contrast_agent.log");
        yield return new V1EnvVar("CONTRAST_INSTALLATION_TOOL", "KUBERNETES_OPERATOR");
    }

    public void PatchContainer(V1Container container, PatchingContext context)
    {
        // Only modify this if CONTRAST_EXISTING_PYTHONPATH isn't already set. This is to prevent infinite loops.
        if (container.Env.FirstOrDefault("PYTHONPATH") is { Value: { } currentPath }
            && !string.IsNullOrWhiteSpace(currentPath)
            && !currentPath.Contains("contrast/loader", StringComparison.OrdinalIgnoreCase)
            && container.Env.FirstOrDefault("CONTRAST_EXISTING_PYTHONPATH") is null)
        {
            container.Env.AddOrUpdate(new V1EnvVar("CONTRAST_EXISTING_PYTHONPATH", currentPath));
            container.Env.AddOrUpdate(new V1EnvVar("PYTHONPATH",
                $"{GetAgentPythonPath(context)}:{currentPath}"));
        }
    }

    private static string GetAgentPythonPath(PatchingContext context) => $"{context.AgentMountPath}:{context.AgentMountPath}/contrast/loader";

}
