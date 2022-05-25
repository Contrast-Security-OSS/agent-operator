using System;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;

namespace Contrast.K8s.AgentOperator.Core
{
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
                AgentInjectionType.Dummy => "docker.io",
                _ => _repositoryOptions.DefaultRegistry
            };
        }

        public string GetDefaultImageName(AgentInjectionType type)
        {
            return type switch
            {
                AgentInjectionType.DotNetCore => "agent-operator/agents/dotnet-core",
                AgentInjectionType.Java => "agent-operator/agents/java",
                AgentInjectionType.NodeJs => "agent-operator/agents/nodejs",
                AgentInjectionType.Php => "agent-operator/agents/php",
                AgentInjectionType.Dummy => "library/busybox",
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
                "nodejs" => AgentInjectionType.NodeJs,
                "php" => AgentInjectionType.Php,
                "personal-home-page" => AgentInjectionType.Php,
                "dummy" => AgentInjectionType.Dummy,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
