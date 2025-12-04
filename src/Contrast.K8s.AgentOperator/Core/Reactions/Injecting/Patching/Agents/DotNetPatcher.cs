// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;
using k8s.Models;
using NLog;
using System;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;

[UsedImplicitly]
public class DotNetAgentPatcher : IAgentPatcher
{
    private readonly InjectorOptions _injectorOptions;
    public AgentInjectionType Type => AgentInjectionType.DotNetCore;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public DotNetAgentPatcher(InjectorOptions injectorOptions)
    {
        _injectorOptions = injectorOptions;
    }

    public IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
    {
        if (_injectorOptions.EnableEarlyChaining)
        {
            yield return new V1EnvVar { Name = "LD_PRELOAD", Value = GetAgentPreloadPath(context) };
        }
        else
        {
            yield return new V1EnvVar { Name = "CORECLR_PROFILER", Value = "{8B2CE134-0948-48CA-A4B2-80DDAD9F5791}" };
            yield return new V1EnvVar { Name = "CORECLR_PROFILER_PATH", Value = $"{context.AgentMountPath}/runtimes/linux-x64/native/ContrastProfiler.so" };
            yield return new V1EnvVar { Name = "CORECLR_PROFILER_PATH_ARM64", Value = $"{context.AgentMountPath}/runtimes/linux-arm64/native/ContrastProfiler.so" };
            yield return new V1EnvVar { Name = "CORECLR_ENABLE_PROFILING", Value = "1" };
        }

        yield return new V1EnvVar { Name = "CONTRAST_INSTALL_SOURCE", Value = "kubernetes-operator" }; //For backwards compatibility
        yield return new V1EnvVar { Name = "CONTRAST_INSTALLATION_TOOL", Value = "KUBERNETES_OPERATOR" };
        yield return new V1EnvVar { Name = "CONTRAST_CORECLR_INSTALL_DIRECTORY", Value = context.AgentMountPath };
        yield return new V1EnvVar { Name = "CONTRAST_CORECLR_DATA_DIRECTORY", Value = context.WritableMountPath };
        yield return new V1EnvVar { Name = "CONTRAST_CORECLR_LOGS_DIRECTORY", Value = $"{context.WritableMountPath}/logs" };
        yield return new V1EnvVar { Name = "CONTRAST__AGENT__DOTNET__ENABLE_FILE_WATCHING", Value = "false" };
    }

    public void PatchContainer(V1Container container, PatchingContext context)
    {
        //Log a warning if we detect DOTNET_EnableDiagnostics=0 or COMPlus_EnableDiagnostics=0
        //We cant patch these to enable them because it will break a .NET 6.0 read-only container because it will attempt to create the IPC socket
        if (container.Env.FirstOrDefault("DOTNET_EnableDiagnostics")?.Value == "0" ||
            container.Env.FirstOrDefault("COMPlus_EnableDiagnostics")?.Value == "0")
        {
            Logger.Warn($"Detected 'DOTNET_EnableDiagnostics=0' or 'COMPlus_EnableDiagnostics=0' environment variable on '{context.WorkloadNamespace}/{context.WorkloadName}', dotnet-core agent may not attach correctly.");
        }

        // This assumes this patch occurs after our generic patches.
        // Either the users sets this on the pod manually, or we set it from our config file.
        // We also assume the default is true.
        var chainingEnabled = !string.Equals(
            container.Env.FirstOrDefault("CONTRAST__AGENT__DOTNET__ENABLE_CHAINING")?.Value,
            "false",
            StringComparison.OrdinalIgnoreCase
        );

        // Only modify this if CONTRAST_EXISTING_LD_PRELOAD isn't already set, or we are not already set from early chaining. This is to prevent infinite loops.
        if (chainingEnabled
            && container.Env.FirstOrDefault("LD_PRELOAD") is { Value: { } currentLdPreloadValue }
            && !string.IsNullOrWhiteSpace(currentLdPreloadValue)
            && !currentLdPreloadValue.Contains("ContrastChainLoader.so", StringComparison.OrdinalIgnoreCase)
            && container.Env.FirstOrDefault("CONTRAST_EXISTING_LD_PRELOAD") is null)
        {
            container.Env.AddOrUpdate(new V1EnvVar { Name = "CONTRAST_EXISTING_LD_PRELOAD", Value = currentLdPreloadValue });
            container.Env.AddOrUpdate(new V1EnvVar { Name = "LD_PRELOAD", Value = $"{GetAgentPreloadPath(context)}:{currentLdPreloadValue}" });
        }
    }

    private static string GetAgentPreloadPath(PatchingContext context) => $"{context.AgentMountPath}/runtimes/linux/native/ContrastChainLoader.so";

}
