// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using JetBrains.Annotations;
using k8s.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

[UsedImplicitly]
public class JavaAgentPatcher : IAgentPatcher
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public AgentInjectionType Type => AgentInjectionType.Java;

    public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
    {
        yield return new V1EnvVar { Name = "JAVA_TOOL_OPTIONS", Value = GetContrastAgentArgument(context) };
        yield return new V1EnvVar { Name = "CONTRAST__AGENT__CONTRAST_WORKING_DIR", Value = context.WritableMountPath };
        yield return new V1EnvVar { Name = "CONTRAST__AGENT__LOGGER__PATH", Value = $"{context.WritableMountPath}/logs/contrast_agent.log" };
        yield return new V1EnvVar { Name = "CONTRAST_INSTALLATION_TOOL", Value = "KUBERNETES_OPERATOR" };

        //Disable hierarchy cache since we are in containers
        yield return new V1EnvVar { Name = "CONTRAST__ASSESS__CACHE__HIERARCHY_ENABLE", Value = "false" };
    }

    public void PatchContainer(V1Container container, PatchingContext context)
    {
        if (container.Env.FirstOrDefault("JAVA_TOOL_OPTIONS") is { Value: { } currentJavaToolOptions }
            && !string.IsNullOrWhiteSpace(currentJavaToolOptions)
            && !currentJavaToolOptions.EndsWith("contrast-agent.jar", StringComparison.OrdinalIgnoreCase)
            && container.Env.FirstOrDefault("CONTRAST_EXISTING_JAVA_TOOL_OPTIONS") is null)
        {
            var contrastAgentArgument = GetContrastAgentArgument(context);

            //Parse and patch the existing JAVA_TOOL_OPTIONS
            container.Env.AddOrUpdate(new V1EnvVar { Name = "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS", Value = currentJavaToolOptions });

            try
            {
                var options = JavaArgumentParser.ParseArguments(currentJavaToolOptions).ToList();

                //Patch contrast-agent.jar to the correct path
                var contrastJavaAgentIndex = options.FindIndex(x => x.StartsWith("-javaagent", StringComparison.OrdinalIgnoreCase)
                                                                    && x.Contains("contrast-agent.jar", StringComparison.OrdinalIgnoreCase));
                if (contrastJavaAgentIndex >= 0)
                {
                    options[contrastJavaAgentIndex] = contrastAgentArgument;
                }
                else //contrast-agent.jar is not in the arguments so just prepend it
                {
                    options.Insert(0, contrastAgentArgument);
                }

                container.Env.AddOrUpdate(new V1EnvVar { Name = "JAVA_TOOL_OPTIONS", Value = string.Join(' ', options) });
            }
            catch (Exception e)
            {
                Logger.Warn(e, "Failed to parse existing JAVA_TOOL_OPTIONS, unable to patch!");
            }
        }
    }

    private static string GetContrastAgentArgument(PatchingContext context) => $"-javaagent:{context.AgentMountPath}/contrast-agent.jar";

    public string GetOverrideAgentMountPath() => "/opt/contrast";
}
