namespace Contrast.K8s.AgentOperator.Options
{
    public record TelemetryOptions(bool Enabled,
                                   string ClusterIdSecretName,
                                   string ClusterIdSecretNamespace);
}
