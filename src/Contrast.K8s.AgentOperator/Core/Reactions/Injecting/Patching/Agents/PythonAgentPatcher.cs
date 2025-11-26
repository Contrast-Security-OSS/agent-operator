// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

[UsedImplicitly]
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
        yield return new V1EnvVar { Name = "PYTHONPATH", Value = GetAgentPythonPath(context) };
        if (_injectorOptions.EnablePythonRewriter)
        {
            yield return new V1EnvVar { Name = "CONTRAST__AGENT__PYTHON__REWRITE", Value = "true" };
        }

        yield return new V1EnvVar { Name = "__CONTRAST_USING_RUNNER", Value = "true" };
        yield return new V1EnvVar { Name = "CONTRAST__AGENT__LOGGER__PATH", Value = $"{context.WritableMountPath}/logs/contrast_agent.log" };
        yield return new V1EnvVar { Name = "CONTRAST_INSTALLATION_TOOL", Value = "KUBERNETES_OPERATOR" };
    }

    public void PatchContainer(V1Container container, PatchingContext context)
    {
        // Only modify this if CONTRAST_EXISTING_PYTHONPATH isn't already set. This is to prevent infinite loops.
        if (container.Env.FirstOrDefault("PYTHONPATH") is { Value: { } currentPath }
            && !string.IsNullOrWhiteSpace(currentPath)
            && !currentPath.EndsWith("contrast/loader", StringComparison.OrdinalIgnoreCase)
            && container.Env.FirstOrDefault("CONTRAST_EXISTING_PYTHONPATH") is null)
        {
            container.Env.AddOrUpdate(new V1EnvVar { Name = "CONTRAST_EXISTING_PYTHONPATH", Value = currentPath });

            //Some operators add the existing PYTHONPATH to the middle of the new PYTHONPATH so remove our existing one and prefix it
            var splitPath = currentPath.Split(':').ToList();
            splitPath.Remove(context.AgentMountPath);
            splitPath.Remove($"{context.AgentMountPath}/contrast/loader");

            container.Env.AddOrUpdate(new V1EnvVar { Name = "PYTHONPATH", Value = $"{GetAgentPythonPath(context)}:{string.Join(':', splitPath)}" });
        }
    }

    private static string GetAgentPythonPath(PatchingContext context) =>
        $"{context.AgentMountPath}:{context.AgentMountPath}/contrast/loader";
}
