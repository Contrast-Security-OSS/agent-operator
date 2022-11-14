// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents
{
    public class JavaAgentPatcher : IAgentPatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public AgentInjectionType Type => AgentInjectionType.Java;

        public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            yield return new V1EnvVar("JAVA_TOOL_OPTIONS", GetContrastAgentArgument(context));
        }

        public void PatchContainer(V1Container container, PatchingContext context)
        {
            if (GetFirstOrDefaultEnvVar(container.Env, "JAVA_TOOL_OPTIONS") is { Value: { } currentJavaToolOptions }
                && !string.IsNullOrWhiteSpace(currentJavaToolOptions)
                && !currentJavaToolOptions.EndsWith("contrast-agent.jar", StringComparison.OrdinalIgnoreCase)
                && GetFirstOrDefaultEnvVar(container.Env, "CONTRAST_EXISTING_JAVA_TOOL_OPTIONS") is null)
            {
                var contrastAgentArgument = GetContrastAgentArgument(context);

                //Parse and patch the existing JAVA_TOOL_OPTIONS
                container.Env.AddOrUpdate(new V1EnvVar("CONTRAST_EXISTING_JAVA_TOOL_OPTIONS", currentJavaToolOptions));

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

                    container.Env.AddOrUpdate(new V1EnvVar("JAVA_TOOL_OPTIONS", string.Join(' ', options)));
                }
                catch (Exception e)
                {
                    Logger.Warn(e, "Failed to parse existing JAVA_TOOL_OPTIONS, unable to patch!");
                }
            }
        }

        private static V1EnvVar? GetFirstOrDefaultEnvVar(IEnumerable<V1EnvVar> collection, string name)
        {
            return collection.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetContrastAgentArgument(PatchingContext context) => $"-javaagent:{context.ContrastMountPath}/contrast-agent.jar";

        public string GetMountPath() => "/opt/contrast";
    }
}
