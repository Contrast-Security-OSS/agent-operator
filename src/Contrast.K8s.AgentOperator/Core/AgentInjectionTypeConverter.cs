using System;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core
{
    public interface IAgentInjectionTypeConverter
    {
        string GetDefaultImageName(AgentInjectionType type);
        AgentInjectionType GetTypeFromString(string type);
    }

    public class AgentInjectionTypeConverter : IAgentInjectionTypeConverter
    {
        public string GetDefaultImageName(AgentInjectionType type)
        {
            return type switch
            {
                AgentInjectionType.DotNetCore => "agent-operator/agents/dotnet-core",
                AgentInjectionType.Java => "agent-operator/agents/java",
                AgentInjectionType.NodeJs => "agent-operator/agents/nodejs",
                AgentInjectionType.Php => "agent-operator/agents/php",
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
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
