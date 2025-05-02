// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;

namespace Contrast.K8s.AgentOperator.Core;

public interface IAgentInjectionTypeConverter
{
    string GetDefaultImageRegistry(AgentInjectionType type);
    string GetDefaultImageName(AgentInjectionType type);
    AgentInjectionType GetTypeFromString(string type);
}

public class AgentInjectionTypeConverter : IAgentInjectionTypeConverter
{
    private readonly ImageRepositoryOptions _repositoryOptions;

    public AgentInjectionTypeConverter(ImageRepositoryOptions repositoryOptions)
    {
        _repositoryOptions = repositoryOptions;
    }

    public string GetDefaultImageRegistry(AgentInjectionType type)
    {
        return type switch
        {
            AgentInjectionType.Dummy => "docker.io/library",
            _ => _repositoryOptions.DefaultRegistry
        };
    }

    public string GetDefaultImageName(AgentInjectionType type)
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
            AgentInjectionType.Dummy => "busybox",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public AgentInjectionType GetTypeFromString(string type)
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
            "dummy" => AgentInjectionType.Dummy,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
