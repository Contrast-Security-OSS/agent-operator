namespace Contrast.K8s.AgentOperator.Options
{
    public record MutatingWebHookOptions(string ConfigurationName, string WebHookName = "pods.agents.contrastsecurity.com");
}
