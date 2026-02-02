// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core;

public static class AgentInjectionTypeConverter
{
    public static string GetDefaultImageName(AgentInjectionType type)
    {
        return type switch
        {
            AgentInjectionType.DotNetCore => "agent-dotnet-core",
            AgentInjectionType.Java => "agent-java",
            AgentInjectionType.NodeJs => "agent-nodejs",
            AgentInjectionType.NodeJsEsm => "agent-nodejs",
            AgentInjectionType.NodeJsLegacy => "agent-nodejs",
            AgentInjectionType.Php => "agent-php",
            AgentInjectionType.Python => "agent-python",
            AgentInjectionType.Flex => "agent-flex",
            AgentInjectionType.Dummy => "agent-dummy",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static AgentInjectionType GetTypeFromString(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "dotnet" => AgentInjectionType.DotNetCore,
            "dotnet-core" => AgentInjectionType.DotNetCore,
            "java" => AgentInjectionType.Java,
            "node" => AgentInjectionType.NodeJs,
            "node-esm" => AgentInjectionType.NodeJsEsm,
            "nodejs" => AgentInjectionType.NodeJs,
            "nodejs-esm" => AgentInjectionType.NodeJsEsm,
            "nodejs-legacy" => AgentInjectionType.NodeJsLegacy,
            "php" => AgentInjectionType.Php,
            "personal-home-page" => AgentInjectionType.Php,
            "python" => AgentInjectionType.Python,
            "flex" => AgentInjectionType.Flex,
            "dummy" => AgentInjectionType.Dummy,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static string GetStringFromType(AgentInjectionType? type)
    {
        return type switch
        {
            AgentInjectionType.DotNetCore => "dotnet-core", 
            AgentInjectionType.Java => "java", 
            AgentInjectionType.NodeJs => "nodejs", 
            AgentInjectionType.NodeJsEsm => "nodejs-esm", 
            AgentInjectionType.NodeJsLegacy => "nodejs-legacy", 
            AgentInjectionType.Php => "php", 
            AgentInjectionType.Python => "python", 
            AgentInjectionType.Flex => "flex",
            AgentInjectionType.Dummy => "dummy", 
            _ => "unknown"
        };
    }
}
