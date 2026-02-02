// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public static class ClusterDefaults
{
    public const string DefaultTokenSecretKey = "token";
    public const string DefaultUsernameSecretKey = "username";
    public const string DefaultApiKeySecretKey = "api-key";
    public const string DefaultServiceKeySecretKey = "service-key";

    public static string AgentConfigurationName(string targetNamespace)
    {
        return "default-agent-configuration-" + HashHelper.GetShortHash(targetNamespace);
    }

    public static string AgentConnectionName(string targetNamespace)
    {
        return "default-agent-connection-" + HashHelper.GetShortHash(targetNamespace);
    }

    public static string AgentConnectionSecretName(string targetNamespace)
    {
        return "default-agent-connection-secret-" + HashHelper.GetShortHash(targetNamespace);
    }

    public static string AgentInjectorName(string targetNamespace, AgentInjectionType agentType)
    {
        var type = AgentInjectionTypeConverter.GetStringFromType(agentType);
        return $"default-agent-injector-{type}-{HashHelper.GetShortHash(targetNamespace)}";
    }

    public static string AgentInjectorPullSecretName(string targetNamespace, AgentInjectionType agentType)
    {
        var type = AgentInjectionTypeConverter.GetStringFromType(agentType);
        return $"default-agent-injector-pullsecret-{type}-{HashHelper.GetShortHash(targetNamespace)}";
    }

    public static string AgentInjectorConfigurationName(string targetNamespace, AgentInjectionType agentType)
    {
        var type = AgentInjectionTypeConverter.GetStringFromType(agentType);
        return $"default-agent-injector-configuration-{type}-{HashHelper.GetShortHash(targetNamespace)}";
    }

    public static string AgentInjectorConnectionName(string targetNamespace, AgentInjectionType agentType)
    {
        var type = AgentInjectionTypeConverter.GetStringFromType(agentType);
        return $"default-agent-injector-connection-{type}-{HashHelper.GetShortHash(targetNamespace)}";
    }

    public static string AgentInjectorConnectionSecretName(string targetNamespace, AgentInjectionType agentType)
    {
        var type = AgentInjectionTypeConverter.GetStringFromType(agentType);
        return $"default-agent-injector-connectionsecret-{type}-{HashHelper.GetShortHash(targetNamespace)}";
    }
}
